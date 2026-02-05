/*
 * TALENT PRICING MANAGEMENT - MIGRATION 000
 * 
 * Description: 
 *   Initial base schema required for the system to function.
 *   Defines the basic users and talent_profiles tables as they existed
 *   before the pricing management update.
 *
 * Author: System
 * Date: 2026-02-05
 */

-- ============================================================================
-- 1. BASE USERS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS public.users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- ============================================================================
-- 2. INITIAL TALENT_PROFILES TABLE
-- ============================================================================
-- This matches the "existing structure" described in the task requirements.
CREATE TABLE IF NOT EXISTS public.talent_profiles (
    id SERIAL PRIMARY KEY,
    talent_id INT NOT NULL,
    stage_name VARCHAR(101) NOT NULL,
    picture TEXT NOT NULL,
    slug VARCHAR(101) NOT NULL,
    description VARCHAR(100) NOT NULL,
    bio VARCHAR(150) NULL,
    price INT NULL, -- Legacy column to be dropped in 001
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT fk_talent_profiles_talent FOREIGN KEY (talent_id) 
        REFERENCES public.users(id)
);

-- Indices for initial table
CREATE UNIQUE INDEX IF NOT EXISTS idx_talent_profiles_talent_id ON talent_profiles(talent_id);

-- ============================================================================
-- 3. SEED DATA
-- ============================================================================
-- Add a sample user and profile for testing purposes
INSERT INTO public.users (id, name, email)
VALUES (123, 'Test Talent', 'talent@example.com')
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.talent_profiles (talent_id, stage_name, picture, slug, description, price)
VALUES (123, 'Test Star', 'https://example.com/pic.jpg', 'test-star', 'Amazing talent for your events', 5000)
ON CONFLICT (talent_id) DO NOTHING;
