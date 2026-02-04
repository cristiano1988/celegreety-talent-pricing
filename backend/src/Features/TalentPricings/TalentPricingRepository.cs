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
            "fn_upsert_talent_pricing",
            parameters,
            commandType: CommandType.StoredProcedure
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
            "fn_upsert_talent_pricing_with_history",
            parameters,
            commandType: CommandType.StoredProcedure
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
            "fn_insert_pricing_history",
            parameters,
            commandType: CommandType.StoredProcedure
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
            sql: "fn_get_talent_pricing_with_history",
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

                if (history != null)
                {
                    dto.History.Add(history);
                }

                return dto;
            },
            param: parameters,
            splitOn: "history_personal_price",
            commandType: CommandType.StoredProcedure
        );

        return lookup.Values.FirstOrDefault();
    }
}