# Talent Pricing Database

## Schema Design

### Tables
- **talent_profiles**: Core data for talent pricing. 
    - `talent_id`: References `public.users(id)`.
    - `personal_price`/`business_price`: Stored as integers in cents/smallest currency unit.
    - `stripe_product_id`: The canonical product reference in Stripe.
    - `version`: Optimistic concurrency control counter.
- **pricing_history**: Immutable audit log.

### Functions
We use PL/pgSQL functions for critical operations:
1. `fn_upsert_talent_pricing`: Core upsert logic with version checks.
2. `fn_insert_pricing_history`: Audit trail creation.
3. `fn_upsert_talent_pricing_with_history`: Atomic wrapper for both.
4. `fn_get_talent_pricing_with_history`: Optimized read query.

### Migrations
1. `001_pricing.sql`: Initial schema.
2. `002_add_versioning.sql`: Version support.
3. `003_composite_upsert.sql`: Atomicity support.
4. `004_unique_constraint.sql`: Data integrity.
5. `005_refine_get_function.sql`: Crash prevention for NULL data.
