/*
 * TALENT PRICING MANAGEMENT - MIGRATION 005
 * 
 * Description: 
 *   Refines the GET function to exclude profiles that exist in the database 
 *   but do not have pricing set yet (NULL values). This ensures the API 
 *   returns 404/Empty instead of crashing during Dapper mapping.
 *
 * Author: System
 * Date: 2026-02-04
 */

DROP FUNCTION IF EXISTS fn_get_talent_pricing_with_history(INT, INT);
CREATE OR REPLACE FUNCTION fn_get_talent_pricing_with_history(
    p_talent_id INT,
    p_limit INT DEFAULT 10
)
RETURNS TABLE (
    talent_id INT,
    stage_name VARCHAR,
    stripe_product_id VARCHAR,
    personal_price INT,
    business_price INT,
    stripe_personal_price_id VARCHAR,
    stripe_business_price_id VARCHAR,
    prices_last_synced_at timestamptz,
    version INT,
    PersonalPrice INT,
    BusinessPrice INT,
    ChangeReason VARCHAR,
    CreatedAt timestamptz
)
LANGUAGE sql
AS $$
    SELECT
        tp.talent_id,
        tp.stage_name,
        tp.stripe_product_id,
        tp.personal_price,
        tp.business_price,
        tp.stripe_personal_price_id,
        tp.stripe_business_price_id,
        tp.prices_last_synced_at,
        tp.version,
        ph.personal_price AS PersonalPrice,
        ph.business_price AS BusinessPrice,
        ph.change_reason AS ChangeReason,
        ph.created_at AS CreatedAt
    FROM talent_profiles tp
    LEFT JOIN pricing_history ph
        ON ph.talent_id = tp.talent_id
    WHERE tp.talent_id = p_talent_id 
      AND tp.personal_price IS NOT NULL -- Critical safeguard against NULL mapping crashes
    ORDER BY ph.created_at DESC
    LIMIT p_limit;
$$;
