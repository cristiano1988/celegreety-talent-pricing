# Talent Pricing Backend

## Implementation Details

### Stack
- **Web API**: ASP.NET Core 8
- **ORM**: Dapper (for high-performance SQL)
- **Mediator**: MediatR (CQRS)
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Stripe**: official Stripe.net SDK

### Features
1. **CQRS Architecture**: Separation of read and write models. 
2. **Options Pattern**: Secure and strongly-typed configuration for Stripe settings.
3. **Resilience**: Polly-based retry policy for external Stripe calls.
4. **Atomic Transactions**: Leverages PostgreSQL functions for single-roundtrip updates and history logging.
5. **Dapper Mapping**: Configured to support snake_case database columns.
6. **Error Handling**: Global exception handling with ProblemDetails output.

### Local Development
1. Ensure PostgreSQL is running.
2. Initialize `appsettings.json` from `appsettings.Example.json`.
3. Run `dotnet run` in `src/`.
4. Tests: `dotnet test ../tests/TalentPricing.UnitTests`.
