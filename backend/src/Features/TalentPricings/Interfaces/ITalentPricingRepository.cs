using Features.TalentPricings.Models;

namespace Features.TalentPricings.Interfaces;

public interface ITalentPricingRepository
{
    Task<int> UpsertTalentPricingAsync(TalentPricingDto pricing);

    Task InsertPricingHistoryAsync(
        int talentId,
        int personalPrice,
        int businessPrice,
        string stripeProductId,
        string stripePersonalPriceId,
        string stripeBusinessPriceId,
        string? changeReason = null);

    Task<TalentPricingWithHistoryDto?> GetTalentPricingWithHistoryAsync(
        int talentId,
        int limit = 10);
}