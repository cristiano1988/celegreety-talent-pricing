/*
 * TALENT PRICING MANAGEMENT - MIGRATION 001
 * 
 * Description: 
 *   Updates the talent_profiles table to support Stripe integration and 
 *   creates the audit log table for pricing history.
 *
 * Author: System
 * Date: 2026-02-02
 */

-- ============================================================================
-- 1. MODIFY TALENT_PROFILES TABLE
-- ============================================================================
-- Remove the legacy 'price' column and add support for:
-- - Separated Personal and Business pricing
-- - Stripe Product and Price IDs
-- - Synchronization metadata
ALTER TABLE talent_profiles 
ADD COLUMN IF NOT EXISTS stripe_product_id VARCHAR(50),
ADD COLUMN IF NOT EXISTS personal_price INT,
ADD COLUMN IF NOT EXISTS business_price INT,
ADD COLUMN IF NOT EXISTS stripe_personal_price_id VARCHAR(50),
ADD COLUMN IF NOT EXISTS stripe_business_price_id VARCHAR(50),
ADD COLUMN IF NOT EXISTS prices_last_synced_at TIMESTAMPTZ;

-- Relax constraints to support pricing-only updates
ALTER TABLE talent_profiles 
ALTER COLUMN stage_name DROP NOT NULL,
ALTER COLUMN picture DROP NOT NULL,
ALTER COLUMN slug DROP NOT NULL,
ALTER COLUMN description DROP NOT NULL;

-- Migrate legacy price data if it exists
UPDATE talent_profiles 
SET personal_price = price,
    business_price = price
WHERE price IS NOT NULL;

-- Now safe to drop legacy column
ALTER TABLE talent_profiles 
DROP COLUMN IF EXISTS price;

-- ============================================================================
-- 2. CREATE PRICING_HISTORY TABLE
-- ============================================================================
-- immutable audit log for all pricing changes
CREATE TABLE IF NOT EXISTS pricing_history (
    id SERIAL PRIMARY KEY,
    talent_id INT NOT NULL REFERENCES public.users(id),
    personal_price INT NOT NULL,
    business_price INT NOT NULL,
    stripe_product_id VARCHAR(50) NOT NULL,
    stripe_personal_price_id VARCHAR(50) NOT NULL,
    stripe_business_price_id VARCHAR(50) NOT NULL,
    change_reason VARCHAR(200),
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

-- Indexes for performance optimization on common lookups
CREATE INDEX IF NOT EXISTS idx_pricing_history_talent_id ON pricing_history(talent_id);
CREATE INDEX IF NOT EXISTS idx_pricing_history_created_at ON pricing_history(created_at);

-- ============================================================================
-- 3. STORED FUNCTIONS
-- ============================================================================

/*
 * Function: fn_upsert_talent_pricing
 * Purpose: Insert or Update talent pricing information.
 * Returns: The ID of the affected talent_profiles record.
 * Validations: Ensures Business Price >= Personal Price.
 */
CREATE OR REPLACE FUNCTION fn_upsert_talent_pricing(
    p_talent_id INT,
    p_stripe_product_id VARCHAR,
    p_personal_price INT,
    p_business_price INT,
    p_stripe_personal_price_id VARCHAR,
    p_stripe_business_price_id VARCHAR
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT;
BEGIN
    -- Business Validation
    IF p_business_price < p_personal_price THEN
        RAISE EXCEPTION 'Business price must be greater or equal to personal price';
    END IF;

    -- Upsert Logic
    INSERT INTO talent_profiles (
        talent_id,
        stripe_product_id,
        personal_price,
        business_price,
        stripe_personal_price_id,
        stripe_business_price_id,
        prices_last_synced_at
    )
    VALUES (
        p_talent_id,
        p_stripe_product_id,
        p_personal_price,
        p_business_price,
        p_stripe_personal_price_id,
        p_stripe_business_price_id,
        now()
    )
    ON CONFLICT (talent_id)
    DO UPDATE SET
        stripe_product_id = EXCLUDED.stripe_product_id,
        personal_price = EXCLUDED.personal_price,
        business_price = EXCLUDED.business_price,
        stripe_personal_price_id = EXCLUDED.stripe_personal_price_id,
        stripe_business_price_id = EXCLUDED.stripe_business_price_id,
        prices_last_synced_at = now()
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

/*
 * Function: fn_insert_pricing_history
 * Purpose: Write a new record to the audit log.
 * Notes: Should be called whenever pricing changes.
 */
CREATE OR REPLACE FUNCTION fn_insert_pricing_history(
    p_talent_id INT,
    p_personal_price INT,
    p_business_price INT,
    p_stripe_product_id VARCHAR,
    p_stripe_personal_price_id VARCHAR,
    p_stripe_business_price_id VARCHAR,
    p_change_reason VARCHAR DEFAULT NULL
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO pricing_history (
        talent_id,
        personal_price,
        business_price,
        stripe_product_id,
        stripe_personal_price_id,
        stripe_business_price_id,
        change_reason,
        created_at
    )
    VALUES (
        p_talent_id,
        p_personal_price,
        p_business_price,
        p_stripe_product_id,
        p_stripe_personal_price_id,
        p_stripe_business_price_id,
        p_change_reason,
        now()
    );
END;
$$;

/*
 * Function: fn_get_talent_pricing_with_history
 * Purpose: Fetch current pricing and recent history in a single query.
 * Returns: Joined result set of current profile + history rows.
 * Limit: Defaults to 10 history items.
 */
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