# Talent Pricing Management System

## Overview
This project implements a robust full-stack solution for managing talent pricing (Personal & Business) with seamless **Stripe Integration**. It is designed to be scalable, maintainable, and easy to audit.

### Key Features
- **Dynamic Pricing**: Support for separate Personal and Business pricing tiers.
- **Stripe Synchronization**: Automatic creation and updating of Stripe Products and Prices.
- **Audit Logging**: Full history tracking of all price changes (`pricing_history`).
- **Validation**: Enforces business rules (e.g., Business Price >= Personal Price).
- **Modern UI**: Responsive Vue 3 frontend with real-time feedback.

---

## 🏗 Architecture

The solution follows **Clean Architecture** principles to ensure separation of concerns:

### Backend (.NET 8)
- **Pattern**: CQRS with MediatR.
- **Data Access**: Dapper for high-performance SQL execution.
- **Database**: PostgreSQL with logic encapsulated in Stored Functions for integrity.
- **Integration**: Strong-typed Stripe Service layer.

### Frontend (Vue 3)
- **Framework**: Vue 3 (Composition API) + TypeScript.
- **Styling**: TailwindCSS + DaisyUI.
- **State**: Composable-based state management (`useTalentPricing`).

---

## 🚀 Getting Started

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (optional, for local dev)
- Node.js 18+ (optional, for local dev)

### Quick Start (Docker)
The easiest way to run the entire stack is via Docker Compose:

```bash
docker-compose up --build
```

The application will be available at:
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000/swagger

### Manual Setup

#### 1. Database
Ensure PostgreSQL is running and update the connection string in `appsettings.json`.
Run the migration script located at:
`database/migrations/001_pricing.sql`

#### 2. Backend
Configure your Stripe keys in `backend/src/appsettings.json`:
```json
"Stripe": {
  "SecretKey": "sk_test_..."
}
```
Run the API:
```bash
cd backend/src
dotnet run
```

#### 3. Frontend
```bash
cd frontend
npm install
npm run dev
```

---

## 🧪 Testing

### Unit Tests
A unit test project is included to verify core business logic and command handlers.
```bash
dotnet test backend/tests/TalentPricing.UnitTests
```

### API Testing
A Postman collection is provided in the `docs/` folder for end-to-end API testing.

---

## 📝 Design Decisions & Assumptions

1.  **Database Logic**: Core data modification logic is placed in PostgreSQL functions (`fn_upsert...`) to ensure data consistency and allow for potential usage by other services without duplicating logic.
2.  **Stripe Sync**: We treat Stripe as a downstream dependency. If Stripe fails, the transaction is rolled back (logical consistency).
3.  **Audit Log**: The `pricing_history` table is immutable and indexed for fast retrieval of historical data.
4.  **Validation**: Validation exists on both Frontend (UX) and Backend (Domain Integrity).

---

## Contact
**Candidate**: [Your Name]