/*
 * TALENT PRICING MANAGEMENT - MIGRATION 004
 * 
 * Description: 
 *   Ensures the talent_id column has a UNIQUE constraint to prevent 
 *   duplicate pricing profiles for the same talent.
 *
 * Author: System
 * Date: 2026-02-04
 */

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'talent_profiles_talent_id_key'
    ) THEN
        ALTER TABLE talent_profiles 
        ADD CONSTRAINT talent_profiles_talent_id_key UNIQUE (talent_id);
    END IF;
END $$;
