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


    public UpdateTalentPricingHandler(
        ITalentPricingRepository repository,
        IStripeService stripe)
    {
        _repository = repository;
        _stripe = stripe;
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

        // 2. Archive old Stripe prices
        await _stripe.ArchivePriceAsync(currentPricing.StripePersonalPriceId);
        await _stripe.ArchivePriceAsync(currentPricing.StripeBusinessPriceId);

        string? newPersonalPriceId = null;
        string? newBusinessPriceId = null;

        try
        {
            // 3. Create new Stripe prices
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


            // 4. Update DB
            var updated = new TalentPricingDto
            {
                TalentId = request.TalentId,
                StripeProductId = currentPricing.StripeProductId,
                PersonalPrice = request.PersonalPrice,
                BusinessPrice = request.BusinessPrice,
                StripePersonalPriceId = newPersonalPriceId,
                StripeBusinessPriceId = newBusinessPriceId
            };

            await _repository.UpsertTalentPricingAsync(updated, request.Version);


            // 5. Audit log
            await _repository.InsertPricingHistoryAsync(
                request.TalentId,
                request.PersonalPrice,
                request.BusinessPrice,
                currentPricing.StripeProductId,
                newPersonalPriceId,
                newBusinessPriceId,
                request.ChangeReason);
        }
        catch (Exception)
        {
            // Compensating Transaction: Archive newly created prices
            // If we fail to save the new pricing to our DB, we should disable the 
            // pricing we just created in Stripe to avoid having "active" but unused prices.
            if (!string.IsNullOrEmpty(newPersonalPriceId)) try { await _stripe.ArchivePriceAsync(newPersonalPriceId); } catch {}
            if (!string.IsNullOrEmpty(newBusinessPriceId)) try { await _stripe.ArchivePriceAsync(newBusinessPriceId); } catch {}
            
            // NOTE: We do not un-archive the OLD prices here because Stripe API doesn't easily support "un-archiving".
            // Ideally, we would only archive the old prices AFTER successful DB update, 
            // but Stripe doesn't support atomic batch operations like that.
            // For now, this compensation ensures we don't leave NEW garbage.
            
            throw;
        }
    }
}