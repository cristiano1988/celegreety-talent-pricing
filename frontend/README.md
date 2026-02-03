# Talent Pricing Management


Interview assignment – Talent pricing management system with Stripe integration.


## Overview


This project implements a full-stack pricing management solution for talents, supporting:
- Personal & Business pricing
- Stripe product & price synchronization
- Audit logging of pricing changes


The architecture follows clean separation of concerns and production-grade patterns.


---


## Tech Stack


**Backend**
- .NET 8
- MediatR (CQRS)
- Dapper
- PostgreSQL
- Stripe API (Test Mode)


**Frontend**
- Vue 3
- TypeScript
- Vite
- TailwindCSS + DaisyUI


**Infrastructure**
- Docker
- Docker Compose


---


## Architecture Overview


- Database logic is encapsulated in PostgreSQL functions
- Business logic handled via CQRS handlers
- Stripe integration isolated in service layer
- Frontend separated into UI components and composables


---


## Running the project


### Prerequisites
- Docker
- Docker Compose


### Start


```bash
docker-compose up --build