# E-Voting System

Web-based electronic voting system (NOUN PGD IT project, Obafemi Emmanuel,
NOU254200791). React SPA frontend + ASP.NET Core 8 Web API backend + SQL
Server 2022. Voters authenticate with email + password, receive a JWT, and
cast one anonymised vote per election. Tallies stream live via SignalR.

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

Secrets (JWT signing key, DB connection string, first-admin credentials) are
read from environment variables / user-secrets, never from
`appsettings.json`. Configure them with:

```
cd backend/src/EVoting.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<value>"
dotnet user-secrets set "ConnectionStrings:Default" "<value>"
dotnet user-secrets set "SeedAdmin:Email" "<value>"
dotnet user-secrets set "SeedAdmin:Password" "<value>"
dotnet user-secrets set "Voting:ConfirmationSecret" "<value>"
```

`SeedAdmin:Email`/`SeedAdmin:Password` seed the first Administrator account
on startup (idempotent — skipped once any Administrator exists). That admin
can then create further Officer/Administrator accounts via
`POST api/admin/users`.

`Voting:ConfirmationSecret` is the HMAC key used to derive each vote's
confirmation hash (from VoteId + ElectionId) — verifiable by recomputing it
with the same secret, but not reversible, and deliberately excludes which
candidate was chosen.

`SendGrid:ApiKey` is not currently used (no email-sending feature exists
yet) — set it only if/when a feature needs it.

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
    EVoting.Infrastructure  — EF Core, repositories, JWT/hashing services (→ Application, Domain)
    EVoting.API             — controllers, SignalR hubs, DI wiring (→ Application; → Infrastructure for composition root only)
  tests/
    EVoting.UnitTests
    EVoting.IntegrationTests
frontend/                   — React 18 + Vite SPA
plans/                      — per-phase implementation plans
```
