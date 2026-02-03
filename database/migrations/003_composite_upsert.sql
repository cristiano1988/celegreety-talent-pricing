/*
 * TALENT PRICING MANAGEMENT - MIGRATION 003
 * 
 * Description: 
 *   Creates a composite function to handle both pricing upsert and history logging
 *   atomically with a single database call.
 *
 * Author: System
 * Date: 2026-02-04
 */

CREATE OR REPLACE FUNCTION fn_upsert_talent_pricing_with_history(
    p_talent_id INT,
    p_stripe_product_id VARCHAR,
    p_personal_price INT,
    p_business_price INT,
    p_stripe_personal_price_id VARCHAR,
    p_stripe_business_price_id VARCHAR,
    p_change_reason VARCHAR DEFAULT NULL,
    p_expected_version INT DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT;
BEGIN
    -- 1. Perform Upsert (includes business validation and version check)
    v_id := fn_upsert_talent_pricing(
        p_talent_id,
        p_stripe_product_id,
        p_personal_price,
        p_business_price,
        p_stripe_personal_price_id,
        p_stripe_business_price_id,
        p_expected_version
    );

    -- 2. Insert Pricing History
    PERFORM fn_insert_pricing_history(
        p_talent_id,
        p_personal_price,
        p_business_price,
        p_stripe_product_id,
        p_stripe_personal_price_id,
        p_stripe_business_price_id,
        p_change_reason
    );

    RETURN v_id;
END;
$$;
