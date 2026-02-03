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

    public CreateTalentPricingHandler(ITalentPricingRepository repository, IStripeService stripe)
    {
        _repository = repository;
        _stripe = stripe;
    }

    public async Task<CreateTalentPricingResult> Handle(CreateTalentPricingCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.BusinessPrice < request.PersonalPrice)
            throw new ArgumentException("Business price must be greater than or equal to personal price.");

        // 1. Create Stripe Product
        // We use the talent ID as metadata to link back to our system.
        var productId = await _stripe.CreateProductAsync(
            request.TalentId, 
            $"Talent {request.TalentId}"
        );

        // 2. Create Stripe Prices
        // Create separate price objects for Personal and Business tiers.
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

        // 3. Persist to Database
        var pricing = new TalentPricingDto
        {
            TalentId = request.TalentId,
            StripeProductId = productId,
            PersonalPrice = request.PersonalPrice,
            BusinessPrice = request.BusinessPrice,
            StripePersonalPriceId = personalPriceId,
            StripeBusinessPriceId = businessPriceId
        };

        await _repository.UpsertTalentPricingAsync(pricing);

        // 4. Create Audit Log
        await _repository.InsertPricingHistoryAsync(
            request.TalentId,
            request.PersonalPrice,
            request.BusinessPrice,
            productId,
            personalPriceId,
            businessPriceId,
            "Initial pricing setup"
        );

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
}