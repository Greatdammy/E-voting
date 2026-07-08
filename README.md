# E-Voting System

Web-based electronic voting system (NOUN PGD IT project, Obafemi Emmanuel,
NOU254200791). React SPA frontend + ASP.NET Core 8 Web API backend + SQL
Server 2022. Voters authenticate with email + password + email OTP, receive a
JWT, and cast one anonymised vote per election. Tallies stream live via
SignalR.

See `CLAUDE.md` for full architecture, data model, security rules, and API
contract. See `PLAN.md` for the phased build plan.

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server 2022

## Backend

```
cd backend
dotnet build EVoting.sln
dotnet run --project src/EVoting.API
```

Secrets (JWT signing key, SendGrid API key, DB connection string) are read
from environment variables / user-secrets, never from `appsettings.json`.
Configure them with:

```
cd backend/src/EVoting.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<value>"
dotnet user-secrets set "SendGrid:ApiKey" "<value>"
dotnet user-secrets set "ConnectionStrings:Default" "<value>"
```

## Frontend

```
cd frontend
npm install
npm run dev
```

## Project structure

```
backend/
  EVoting.sln
  src/
    EVoting.Domain          — entities, value objects, enums (no dependencies)
    EVoting.Application     — service interfaces, DTOs, validators (→ Domain)
    EVoting.Infrastructure  — EF Core, repositories, email/JWT/OTP services (→ Application, Domain)
    EVoting.API             — controllers, SignalR hubs, DI wiring (→ Application; → Infrastructure for composition root only)
  tests/
    EVoting.UnitTests
    EVoting.IntegrationTests
frontend/                   — React 18 + Vite SPA
plans/                      — per-phase implementation plans
```
