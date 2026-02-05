using MediatR;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Models;
using Services;

namespace Features.TalentPricings.Commands;

public class UpdateTalentPricingHandler
    : IRequestHandler<UpdateTalentPricingCommand>
{
    private readonly ITalentPricingRepository _repository;
    private readonly IStripeService _stripe;
    private readonly ILogger<UpdateTalentPricingHandler> _logger;


    public UpdateTalentPricingHandler(
        ITalentPricingRepository repository,
        IStripeService stripe,
        ILogger<UpdateTalentPricingHandler> logger)
    {
        _repository = repository;
        _stripe = stripe;
        _logger = logger;
    }


    public async Task Handle(
        UpdateTalentPricingCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PersonalPrice <= 0 || request.BusinessPrice <= 0)
            throw new ArgumentException("Prices must be greater than zero.");

        if (request.BusinessPrice < request.PersonalPrice)
            throw new ArgumentException("Business price must be >= personal price");

        // 1. Get current pricing
        var current = await _repository.GetTalentPricingWithHistoryAsync(request.TalentId);
        if (current == null)
            throw new InvalidOperationException("Pricing does not exist");

        var currentPricing = current.Current;
        var productId = currentPricing.StripeProductId;

        string? newPersonalPriceId = null;
        string? newBusinessPriceId = null;

        try
        {
            // 2. Ensure Stripe Product exists (for legacy talents)
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogInformation("Stripe Product ID missing for talent {TalentId}. Creating new product.", request.TalentId);
                var profile = await _repository.GetTalentProfileAsync(request.TalentId);
                productId = await _stripe.CreateProductAsync(
                    request.TalentId, 
                    profile?.StageName ?? $"Talent {request.TalentId}"
                );
                _logger.LogInformation("New Stripe product created: {ProductId}", productId);
            }

            // 3. Create new Stripe prices
            _logger.LogInformation("Creating new Stripe prices for product {ProductId}", productId);
            newPersonalPriceId = await _stripe.CreatePriceAsync(
                productId,
                request.PersonalPrice,
                request.Currency,
                "personal");


            newBusinessPriceId = await _stripe.CreatePriceAsync(
                productId,
                request.BusinessPrice,
                request.Currency,
                "business");
            _logger.LogInformation("New Stripe prices created: {PersonalPriceId}, {BusinessPriceId}", newPersonalPriceId, newBusinessPriceId);


            // 4. Update DB
            var updated = new TalentPricingDto
            {
                TalentId = request.TalentId,
                StripeProductId = productId,
                PersonalPrice = request.PersonalPrice,
                BusinessPrice = request.BusinessPrice,
                StripePersonalPriceId = newPersonalPriceId,
                StripeBusinessPriceId = newBusinessPriceId
            };

            _logger.LogInformation("Persisting atomic pricing update to DB for talent {TalentId}", request.TalentId);
            await _repository.UpsertWithHistoryAsync(updated, request.ChangeReason, request.Version);
            _logger.LogInformation("Successfully updated pricing and audit log for talent {TalentId}", request.TalentId);

            // 4. Archive old Stripe prices (Cleanup task - failure should not block update)
            _logger.LogInformation("Archiving old Stripe prices for talent {TalentId}", request.TalentId);
            try 
            {
                await _stripe.ArchivePriceAsync(currentPricing.StripePersonalPriceId);
                await _stripe.ArchivePriceAsync(currentPricing.StripeBusinessPriceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to archive old Stripe prices for talent {TalentId}. This is a non-critical cleanup failure.", request.TalentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update pricing for talent {TalentId}", request.TalentId);
            
            // Compensating Transaction: Archive newly created prices if they were created
            if (!string.IsNullOrEmpty(newPersonalPriceId)) 
                try { await _stripe.ArchivePriceAsync(newPersonalPriceId); } 
                catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback new personal price {PriceId}", newPersonalPriceId); }
            
            if (!string.IsNullOrEmpty(newBusinessPriceId)) 
                try { await _stripe.ArchivePriceAsync(newBusinessPriceId); } 
                catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback new business price {PriceId}", newBusinessPriceId); }
            
            throw;
        }
    }
}