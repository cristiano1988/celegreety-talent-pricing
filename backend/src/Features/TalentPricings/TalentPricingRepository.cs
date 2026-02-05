// Dapper repository implementation.
// Encapsulates all database interactions calling PostgreSQL functions.

using System.Data;
using Dapper;
using Npgsql;
using Features.TalentPricings.Models;
using Features.TalentPricings.Interfaces;

namespace Features.TalentPricings.Repository;

public class TalentPricingRepository : ITalentPricingRepository
{
    private readonly IDbConnection _db;

    public TalentPricingRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> UpsertTalentPricingAsync(TalentPricingDto pricing, int? expectedVersion = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_talent_id", pricing.TalentId);
        parameters.Add("p_stripe_product_id", pricing.StripeProductId);
        parameters.Add("p_personal_price", pricing.PersonalPrice);
        parameters.Add("p_business_price", pricing.BusinessPrice);
        parameters.Add("p_stripe_personal_price_id", pricing.StripePersonalPriceId);
        parameters.Add("p_stripe_business_price_id", pricing.StripeBusinessPriceId);
        parameters.Add("p_expected_version", expectedVersion);

        return await _db.ExecuteScalarAsync<int>(
            "SELECT fn_upsert_talent_pricing(@p_talent_id, @p_stripe_product_id, @p_personal_price, @p_business_price, @p_stripe_personal_price_id, @p_stripe_business_price_id, @p_expected_version)",
            parameters
        );
    }

    public async Task<int> UpsertWithHistoryAsync(TalentPricingDto pricing, string? changeReason = null, int? expectedVersion = null)
    {
        if (_db.State != ConnectionState.Open) await ((NpgsqlConnection)_db).OpenAsync();
        
        var parameters = new DynamicParameters();
        parameters.Add("p_talent_id", pricing.TalentId);
        parameters.Add("p_stripe_product_id", pricing.StripeProductId);
        parameters.Add("p_personal_price", pricing.PersonalPrice);
        parameters.Add("p_business_price", pricing.BusinessPrice);
        parameters.Add("p_stripe_personal_price_id", pricing.StripePersonalPriceId);
        parameters.Add("p_stripe_business_price_id", pricing.StripeBusinessPriceId);
        parameters.Add("p_change_reason", changeReason);
        parameters.Add("p_expected_version", expectedVersion);

        return await _db.ExecuteScalarAsync<int>(
            "SELECT fn_upsert_talent_pricing_with_history(@p_talent_id, @p_stripe_product_id, @p_personal_price, @p_business_price, @p_stripe_personal_price_id, @p_stripe_business_price_id, @p_change_reason, @p_expected_version)",
            parameters
        );
    }

    public async Task InsertPricingHistoryAsync(
        int talentId,
        int personalPrice,
        int businessPrice,
        string stripeProductId,
        string stripePersonalPriceId,
        string stripeBusinessPriceId,
        string? changeReason = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_talent_id", talentId);
        parameters.Add("p_personal_price", personalPrice);
        parameters.Add("p_business_price", businessPrice);
        parameters.Add("p_stripe_product_id", stripeProductId);
        parameters.Add("p_stripe_personal_price_id", stripePersonalPriceId);
        parameters.Add("p_stripe_business_price_id", stripeBusinessPriceId);
        parameters.Add("p_change_reason", changeReason);

        await _db.ExecuteAsync(
            "SELECT fn_insert_pricing_history(@p_talent_id, @p_personal_price, @p_business_price, @p_stripe_product_id, @p_stripe_personal_price_id, @p_stripe_business_price_id, @p_change_reason)",
            parameters
        );
    }

    public async Task<TalentPricingWithHistoryDto?> GetTalentPricingWithHistoryAsync(int talentId, int limit = 10)
    {
        if (_db.State != ConnectionState.Open) await ((NpgsqlConnection)_db).OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("p_talent_id", talentId);
        // parameters.Add("p_limit", limit); // Removed as 'limit' parameter is no longer in the signature

        var lookup = new Dictionary<int, TalentPricingWithHistoryDto>();

        var result = await _db.QueryAsync<TalentPricingDto, PricingHistoryDto, TalentPricingWithHistoryDto>(
            sql: "SELECT * FROM fn_get_talent_pricing_with_history(@p_talent_id)",
            map: (current, history) =>
            {
                if (!lookup.TryGetValue(current.TalentId, out var dto))
                {
                    dto = new TalentPricingWithHistoryDto
                    {
                        Current = current
                    };
                    lookup.Add(current.TalentId, dto);
                }

                // If LEFT JOIN returns no history, history object will be non-null but with default values (year 0001)
                // We skip adding such objects to the history list.
                if (history != null && history.CreatedAt != default)
                {
                    dto.History.Add(history);
                }

                return dto;
            },
            param: parameters,
            splitOn: "PersonalPrice"
        );

        return lookup.Values.FirstOrDefault();
    }

    public async Task<TalentPricingDto?> GetTalentProfileAsync(int talentId)
    {
        if (_db.State != ConnectionState.Open) await ((NpgsqlConnection)_db).OpenAsync();
        return await _db.QueryFirstOrDefaultAsync<TalentPricingDto>(
            "SELECT talent_id as TalentId, stage_name as StageName, stripe_product_id as StripeProductId, personal_price as PersonalPrice, business_price as BusinessPrice, stripe_personal_price_id as StripePersonalPriceId, stripe_business_price_id as StripeBusinessPriceId, version FROM talent_profiles WHERE talent_id = @talentId",
            new { talentId }
        );
    }

    public async Task<bool> TalentExistsAsync(int talentId)
    {
        if (_db.State != ConnectionState.Open) await ((NpgsqlConnection)_db).OpenAsync();
        // Assuming 'users' table exists since it's referenced by FK in pricing_history
        return await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM public.users WHERE id = @talentId)",
            new { talentId }
        );
    }
}