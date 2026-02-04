// MediatR command handler
// Orchestrates Stripe product/price creation and repository persistence.
// Ensures transactional consistency via logical ordering (Stripe -> DB).

using MediatR;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Models;
using Services;

namespace Features.TalentPricings.Commands;

public class CreateTalentPricingHandler : IRequestHandler<CreateTalentPricingCommand, CreateTalentPricingResult>
{
    private readonly ITalentPricingRepository _repository;
    private readonly IStripeService _stripe;
    private readonly ILogger<CreateTalentPricingHandler> _logger;

    public CreateTalentPricingHandler(ITalentPricingRepository repository, IStripeService stripe, ILogger<CreateTalentPricingHandler> logger)
    {
        _repository = repository;
        _stripe = stripe;
        _logger = logger;
    }

    public async Task<CreateTalentPricingResult> Handle(CreateTalentPricingCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.PersonalPrice <= 0 || request.BusinessPrice <= 0)
            throw new ArgumentException("Prices must be greater than zero.");

        if (request.BusinessPrice < request.PersonalPrice)
            throw new ArgumentException("Business price must be greater than or equal to personal price.");

        // 0. Existence & Idempotency Check
        _logger.LogInformation("Verifying talent {TalentId} existence", request.TalentId);
        if (!await _repository.TalentExistsAsync(request.TalentId))
            throw new InvalidOperationException($"Talent with ID {request.TalentId} does not exist.");

        var existingProfile = await _repository.GetTalentProfileAsync(request.TalentId);
        if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.StripeProductId))
        {
            _logger.LogWarning("Talent {TalentId} already has a Stripe product {ProductId}. Use Update instead.", request.TalentId, existingProfile.StripeProductId);
            throw new InvalidOperationException($"Pricing already exists for talent {request.TalentId}. Please use the Update endpoint.");
        }

        string? productId = null;
        try
        {
            _logger.LogInformation("Creating Stripe product for talent {TalentId}", request.TalentId);
            productId = await _stripe.CreateProductAsync(
                request.TalentId, 
                $"Talent {request.TalentId}"
            );
            _logger.LogInformation("Stripe product created: {ProductId}", productId);

            _logger.LogInformation("Creating Stripe prices for product {ProductId}", productId);
            var personalPriceId = await _stripe.CreatePriceAsync(
                productId,
                request.PersonalPrice,
                request.Currency,
                "personal"
            );

            var businessPriceId = await _stripe.CreatePriceAsync(
                productId,
                request.BusinessPrice,
                request.Currency,
                "business"
            );
            _logger.LogInformation("Stripe prices created: {PersonalPriceId}, {BusinessPriceId}", personalPriceId, businessPriceId);

            var pricing = new TalentPricingDto
            {
                TalentId = request.TalentId,
                StripeProductId = productId,
                PersonalPrice = request.PersonalPrice,
                BusinessPrice = request.BusinessPrice,
                StripePersonalPriceId = personalPriceId,
                StripeBusinessPriceId = businessPriceId
            };

            await _repository.UpsertWithHistoryAsync(pricing, "Initial pricing setup");
            _logger.LogInformation("Successfully created pricing and audit log for talent {TalentId}", request.TalentId);

            return new CreateTalentPricingResult
            {
                TalentId = request.TalentId,
                StripeProductId = productId,
                PersonalPrice = request.PersonalPrice,
                BusinessPrice = request.BusinessPrice,
                StripePersonalPriceId = personalPriceId,
                StripeBusinessPriceId = businessPriceId,
                LastSyncedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create talent pricing for talent {TalentId}", request.TalentId);
            // Compensating Transaction: Rollback Stripe changes
            // If we successfully created a Stripe product but failed to save to our DB,
            // we archive the product to prevent "ghost" products in Stripe.
            if (!string.IsNullOrEmpty(productId))
            {
                _logger.LogWarning("Rolling back Stripe product {ProductId} due to failure", productId);
                // We don't need to await this if we want fail-fast, but better to ensure cleanup.
                // Swallowing any error here to ensure the original exception bubbles up.
                try { await _stripe.ArchiveProductAsync(productId); } catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback Stripe product {ProductId}", productId); }
            }
            throw;
        }
    }
}