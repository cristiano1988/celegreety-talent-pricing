/*
 * TALENT PRICING MANAGEMENT - MIGRATION 002
 * 
 * Description: 
 *   Adds optimistic concurrency control to talent profiles.
 *   - Adds 'version' column to talent_profiles table.
 *   - Updates upsert function to check version and prevent overlapping updates.
 *
 * Author: System
 * Date: 2026-02-03
 */

-- 1. ADD VERSION COLUMN
ALTER TABLE talent_profiles
ADD COLUMN IF NOT EXISTS version INT DEFAULT 1;

-- 2. UPDATE UPSERT FUNCTION
/*
 * Function: fn_upsert_talent_pricing
 * Purpose: Insert or Update talent pricing information with Optimistic Locking.
 * Returns: The ID of the affected talent_profiles record.
 * Throws: 'Concurrent modification' exception (SQLSTATE 'P0001') if versions mismatch.
 */
CREATE OR REPLACE FUNCTION fn_upsert_talent_pricing(
    p_talent_id INT,
    p_stripe_product_id VARCHAR,
    p_personal_price INT,
    p_business_price INT,
    p_stripe_personal_price_id VARCHAR,
    p_stripe_business_price_id VARCHAR,
    p_expected_version INT DEFAULT NULL 
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT;
    v_current_version INT;
BEGIN
    -- Business Validation
    IF p_business_price < p_personal_price THEN
        RAISE EXCEPTION 'Business price must be greater or equal to personal price';
    END IF;

    -- Check for existing record to determine if this is an update vs insert
    SELECT version INTO v_current_version 
    FROM talent_profiles 
    WHERE talent_id = p_talent_id;

    -- Optimistic Concurrency Check (Only for Updates)
    IF v_current_version IS NOT NULL AND p_expected_version IS NOT NULL THEN
        IF v_current_version != p_expected_version THEN
            RAISE EXCEPTION 'Concurrent modification detected. Client Version: %, DB Version: %', p_expected_version, v_current_version
                USING ERRCODE = 'P0001'; -- Custom error code or standard app conflict code
        END IF;
    END IF;

    -- Upsert Logic
    INSERT INTO talent_profiles (
        talent_id,
        stripe_product_id,
        personal_price,
        business_price,
        stripe_personal_price_id,
        stripe_business_price_id,
        prices_last_synced_at,
        version
    )
    VALUES (
        p_talent_id,
        p_stripe_product_id,
        p_personal_price,
        p_business_price,
        p_stripe_personal_price_id,
        p_stripe_business_price_id,
        now(),
        1 -- Initial version
    )
    ON CONFLICT (talent_id)
    DO UPDATE SET
        stripe_product_id = EXCLUDED.stripe_product_id,
        personal_price = EXCLUDED.personal_price,
        business_price = EXCLUDED.business_price,
        stripe_personal_price_id = EXCLUDED.stripe_personal_price_id,
        stripe_business_price_id = EXCLUDED.stripe_business_price_id,
        prices_last_synced_at = now(),
        version = talent_profiles.version + 1
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

-- 3. UPDATE GET FUNCTION TO RETURN VERSION
DROP FUNCTION IF EXISTS fn_get_talent_pricing_with_history(INT, INT);
CREATE OR REPLACE FUNCTION fn_get_talent_pricing_with_history(
    p_talent_id INT,
    p_limit INT DEFAULT 10
)
RETURNS TABLE (
    talent_id INT,
    stripe_product_id VARCHAR,
    personal_price INT,
    business_price INT,
    stripe_personal_price_id VARCHAR,
    stripe_business_price_id VARCHAR,
    prices_last_synced_at timestamptz,
    version INT, -- New column
    history_personal_price INT,
    history_business_price INT,
    history_change_reason VARCHAR,
    history_created_at timestamptz
)
LANGUAGE sql
AS $$
    SELECT
        tp.talent_id,
        tp.stripe_product_id,
        tp.personal_price,
        tp.business_price,
        tp.stripe_personal_price_id,
        tp.stripe_business_price_id,
        tp.prices_last_synced_at,
        tp.version,
        ph.personal_price AS history_personal_price,
        ph.business_price AS history_business_price,
        ph.change_reason AS history_change_reason,
        ph.created_at AS history_created_at
    FROM talent_profiles tp
    LEFT JOIN pricing_history ph
        ON ph.talent_id = tp.talent_id
    WHERE tp.talent_id = p_talent_id
    ORDER BY ph.created_at DESC
    LIMIT p_limit;
$$;
