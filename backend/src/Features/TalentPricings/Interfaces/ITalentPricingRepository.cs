using Features.TalentPricings.Models;

namespace Features.TalentPricings.Interfaces;

public interface ITalentPricingRepository
{
    Task<int> UpsertWithHistoryAsync(TalentPricingDto pricing, string? changeReason = null, int? expectedVersion = null);

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

    Task<TalentPricingDto?> GetTalentProfileAsync(int talentId);
    Task<bool> TalentExistsAsync(int talentId);
}