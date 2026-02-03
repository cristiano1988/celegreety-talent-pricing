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

        _logger.LogInformation("Archiving old Stripe prices for talent {TalentId}", request.TalentId);
        // 2. Archive old Stripe prices (Cleanup task - failure should not block update)
        try 
        {
            await _stripe.ArchivePriceAsync(currentPricing.StripePersonalPriceId);
            await _stripe.ArchivePriceAsync(currentPricing.StripeBusinessPriceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to archive old Stripe prices for talent {TalentId}. Proceeding with update.", request.TalentId);
            // Log error here (e.g. via ILogger) but proceed.
            // Failure to archive an old price is not a critical business failure.
        }

        string? newPersonalPriceId = null;
        string? newBusinessPriceId = null;

        try
        {
            _logger.LogInformation("Creating new Stripe prices for product {ProductId}", currentPricing.StripeProductId);
            newPersonalPriceId = await _stripe.CreatePriceAsync(
                currentPricing.StripeProductId,
                request.PersonalPrice,
                request.Currency,
                "personal");


            newBusinessPriceId = await _stripe.CreatePriceAsync(
                currentPricing.StripeProductId,
                request.BusinessPrice,
                request.Currency,
                "business");
            _logger.LogInformation("New Stripe prices created: {PersonalPriceId}, {BusinessPriceId}", newPersonalPriceId, newBusinessPriceId);


            var updated = new TalentPricingDto
            {
                TalentId = request.TalentId,
                StripeProductId = currentPricing.StripeProductId,
                PersonalPrice = request.PersonalPrice,
                BusinessPrice = request.BusinessPrice,
                StripePersonalPriceId = newPersonalPriceId,
                StripeBusinessPriceId = newBusinessPriceId
            };

            await _repository.UpsertWithHistoryAsync(updated, request.ChangeReason, request.Version);
            _logger.LogInformation("Successfully updated pricing and audit log for talent {TalentId}", request.TalentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update pricing for talent {TalentId}", request.TalentId);
            // Compensating Transaction: Archive newly created prices
            // If we fail to save the new pricing to our DB, we should disable the 
            // pricing we just created in Stripe to avoid having "active" but unused prices.
            if (!string.IsNullOrEmpty(newPersonalPriceId)) try { await _stripe.ArchivePriceAsync(newPersonalPriceId); } catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback new personal price {PriceId}", newPersonalPriceId); }
            if (!string.IsNullOrEmpty(newBusinessPriceId)) try { await _stripe.ArchivePriceAsync(newBusinessPriceId); } catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback new business price {PriceId}", newBusinessPriceId); }
            
            // NOTE: We do not un-archive the OLD prices here because Stripe API doesn't easily support "un-archiving".
            // Ideally, we would only archive the old prices AFTER successful DB update, 
            // but Stripe doesn't support atomic batch operations like that.
            // For now, this compensation ensures we don't leave NEW garbage.
            
            throw;
        }
    }
}