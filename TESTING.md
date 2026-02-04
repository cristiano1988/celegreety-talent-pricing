# 🧪 Testing Guide

This document provides detailed instructions on how to run tests for the **Talent Pricing Management System**.

## 1. Automated Unit Tests (Backend)

We use **xUnit** and **Moq** to test the business logic of our CQRS handlers. These tests run in isolation and do not require a database or Stripe connection.

### Prerequisites
- .NET 8 SDK installed.

### How to Run
Open a terminal in the project root and run:

```bash
dotnet test backend/tests/TalentPricing.UnitTests
```

### What is Tested?
- **CreateTalentPricingHandler**:
  - ✅ Success: Verifies Stripe Product/Price creation and Database insertion.
  - ✅ Idempotency: Verifies it prevents duplicate Stripe products for the same talent.
  - ✅ Existence: Verifies it checks if the Talent (User) exists in the database.
  - ❌ Failure: Validates that `Business Price >= Personal Price`.
- **UpdateTalentPricingHandler**:
  - ✅ Success: Verifies atomic updates (New Price -> DB Update -> Archive Old).
  - ❌ Failure: Validates business rules, optimistic concurrency (version), and existence.

---

## 2. API Integration Tests (Manual)

We provide a **Postman Collection** to test the end-to-end flow including the Database and Stripe integration.

### Prerequisites
1.  **Docker** is running (`docker-compose up`).
2.  **Stripe Test Keys** are configured in `backend/src/appsettings.json`.
3.  **Postman** is installed.

### Setup
1.  Open Postman.
2.  Click **Import**.
3.  Select the file: `api/TalentPricing.postman_collection.json`.

### Test Scenarios

#### Scene 1: Create New Pricing
1.  Open the request **"Create Pricing"**.
2.  Body is pre-filled with valid data (Talent ID: 123).
3.  Click **Send**.
4.  **Verify**:
    - Status: `200 OK`.
    - Response contains `stripeProductId`, `stripePersonalPriceId`, etc.
    - Check Stripe Dashboard: Product "Talent 123" created?
    - Check Database: `SELECT * FROM talent_profiles WHERE talent_id = 123;`

#### Scene 2: Get Pricing
1.  Open request **"Get Pricing"**.
2.  Ensure URL has correct ID (e.g., `/api/talentpricing/123`).
3.  Click **Send**.
4.  **Verify**:
    - JSON returns current prices AND history array.

#### Scene 3: Update Pricing
1.  Open request **"Update Pricing"**.
2.  Modify the body (e.g., change prices).
3.  Click **Send**.
4.  **Verify**:
    - Status: `204 No Content`.
    - Check Database: `SELECT * FROM pricing_history;` should show a new record.

#### Scene 4: Validation Error
1.  Open **"Create Pricing"**.
2.  Set `businessPrice` lower than `personalPrice` (e.g., 100 vs 200).
3.  Click **Send**.
4.  **Verify**:
    - Status: `400 Bad Request`.
