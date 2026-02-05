# Task: Resolve Frontend Build Error

- [x] Investigate project structure and configuration
    - [x] Read frontend `package.json`
    - [x] Read root and frontend Dockerfiles
- [x] Research `vue-tsc` error "Search string not found: /supportedTSExtensions = .*(?=;)/"
- [x] Propose and implement a fix
    - [x] Update dependencies or apply workaround
- [x] Verify fix by running build

# Task: Resolve Database Migration Error

- [x] Investigate database crash
    - [x] Read migration files
    - [x] Examine PDF requirements
- [x] Implement base schema migration
- [x] Verify database startup and data integrity

# Task: Resolve Connection Error (ECONNREFUSED)

- [x] Investigate proxy error
    - [x] Check `vite.config.ts`
    - [x] Check `docker-compose.yml` environment variables
- [x] Correct API URL in `docker-compose.yml`
- [x] Verify frontend-backend communication

# Task: Configure Stripe Integration

- [x] Store Stripe API Keys
    - [x] Created root `.env` file
- [x] Configure Backend to use Secret Key
- [x] Configure Frontend to use Publishable Key (none required)
- [x] Verify Stripe Service initialization

# Task: Resolve Stripe Update Error (missing product ID)

- [x] Investigate `UpdateTalentPricingHandler` logic
- [x] Modify `UpdateTalentPricingHandler` to create product if missing
- [x] Verify full update flow for legacy talent 123

# Task: Resolve Pricing History Display Error

- [x] Correct SQL aliases in `fn_get_talent_pricing_with_history`
- [x] Update Dapper mapper in `TalentPricingRepository.cs`
- [x] Verify correct history rendering in frontend

# Task: Validate Price Limits (Stripe Compatibility)

- [x] Add backend validation for maximum price (999,999.99)
- [x] Add frontend validation for maximum price
- [x] Verify error handling for excessive prices
- [x] Add explicit frontend validation messages for failing validators

# Task: Fix Docker Build Cache Error

- [x] Prune Docker builder cache
- [x] Rebuild frontend without cache
- [x] Restart all services

# Task: Improve Documentation & Accessibility

- [x] Create visual architecture and sequence diagrams (Mermaid)
- [x] Add beginner-friendly glossary to walkthrough
- [x] Update README.md with visual summaries and beginner notes
- [x] Consolidate technical findings and verification results
- [x] Add `.env.example` and Stripe setup instructions to README.md
- [x] Document all SQL migration files in walkthrough.md

# Task: Refactor Project Structure (Testing)

- [x] Move Postman collection to `tests/integration/`
- [x] Remove redundant root `api` directory
- [x] Update documentation to reflect new paths

# Task: Resolve Authentication Error (500 Internal Server Error)

- [x] Investigate `[Authorize]` usage in controllers
- [x] Determine if Auth is required by scope
- [x] Remove `[Authorize]` or implement Basic/Mock Auth
- [x] Verify API accessibility from frontend

# Task: Resolve Database Function Call Error (procedure does not exist)

- [x] Investigate `CommandType.StoredProcedure` issues with Npgsql
- [x] Update `TalentPricingRepository.cs` to use `CommandType.Text`
- [x] Verify full end-to-end functionality
