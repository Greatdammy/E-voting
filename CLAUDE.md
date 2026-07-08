# CLAUDE.md — E-Voting System

Authoritative context for Claude Code. Read this fully before any task.

## What this is
A web-based electronic voting system (NOUN PGD IT project, OBAFEMI EMMANUEL,
NOU254200791). React.js SPA frontend + ASP.NET Core 8 Web API backend +
SQL Server 2022. Voters authenticate with email + password + email OTP,
receive a JWT, and cast one anonymised vote per election. Tallies stream
live via SignalR.

## Non-negotiable workflow rules
1. **Plan before implementing.** For any non-trivial task, produce a written
   plan in `plans/` and wait for my approval before writing code. Do not
   touch source files during the planning step.
2. **Full method rewrites, not partial diffs.** When changing a method, output
   the entire method body, not a fragment.
3. **EF Core migrations are manual.** Generate the migration command for me to
   run; never auto-apply migrations or alter the database without showing the
   migration first.
4. **No secrets in source.** JWT signing key, SendGrid key, and the DB
   connection string live in environment variables / user-secrets, never in
   committed files. `appsettings.json` holds placeholders only.

## Architecture (must be respected)
Backend = Clean Architecture, strict dependency rule (outer → inner only):
- `EVoting.Domain` — entities, value objects, enums. No dependencies.
- `EVoting.Application` — service interfaces, business logic, DTOs,
  FluentValidation validators. Depends on Domain only.
- `EVoting.Infrastructure` — EF Core `AppDbContext`, repositories, SendGrid
  email service, JWT + OTP + hashing services. Depends on Application + Domain.
- `EVoting.API` — controllers, middleware, SignalR hubs, DI wiring. Depends on
  Application only.

Frontend = React 18 + Vite SPA:
- Redux Toolkit holds auth state (JWT, decoded role, expiry).
- Axios instance with a request interceptor that attaches the JWT.
- `ProtectedRoute` checks the store on every route change; expired/absent
  token → redirect to login before any API call.
- Recharts results dashboard updated by a SignalR client connection.

## Data model (source of truth)
- **Users**: UserId (GUID PK), FullName, Email (unique), PasswordHash (BCrypt),
  Role (Voter/ElectionOfficer/Administrator), IsVerified (default 0), CreatedAt.
- **Elections**: ElectionId (GUID PK), Title, Description, StartDate, EndDate,
  Status (Upcoming/Active/Closed, default Upcoming), CreatedBy (FK Users).
- **Candidates**: CandidateId (GUID PK), ElectionId (FK), Name, Party, PhotoUrl.
- **Votes**: VoteId (GUID PK), ElectionId (FK), VoterId (SHA-256 hash of
  UserId, NVARCHAR(64)), CandidateId (FK), VotedAt, VoteHash (NVARCHAR(64)).
  **UNIQUE (VoterId, ElectionId)** — DB-level one-vote-per-voter enforcement.
- **VoterElectionStatus**: tracks whether a voter has voted in an election.
- **OtpTokens**: hashed OTP, expiry, used flag.
- **AuditLogs**: timestamp, UserId, action description.

## Security rules (treated as design requirements, not afterthoughts)
- Passwords: BCrypt, work factor 12.
- OTP: 6 digits, CSPRNG, stored hashed, 10-minute expiry, invalidated on use.
- JWT: HMAC-SHA256, secret from env var, 8-hour expiry, carries UserId + role.
- Ballot anonymisation: store SHA-256(UserId) as VoterId, never the raw UserId.
- Confirmation hash: derived from VoteId + ElectionId + server secret;
  verifiable, not reversible.
- All input validated via FluentValidation before processing.
- RBAC via `[Authorize(Roles=...)]`; bad/missing token → 401/403 before the
  controller runs.
- HTTPS only, HSTS (1-year max-age), TLS 1.2+.
- Rate limit: auth endpoints capped at 10 requests/min/IP.
- EF Core only for data access — no raw SQL, every query parameterised.

## API surface (build to this contract)
| Method | Endpoint | Role | Purpose |
|---|---|---|---|
| POST | api/auth/register | Public | Register a voter |
| POST | api/auth/login | Public | Authenticate, dispatch OTP |
| POST | api/auth/verify-otp | Public | Verify OTP, issue JWT |
| GET | api/elections | Voter | Eligible elections for the voter |
| GET | api/elections/{id}/ballot | Voter | Ballot for an active election |
| POST | api/elections/{id}/vote | Voter | Submit a vote |
| GET | api/elections/{id}/results | Public | Current tally |
| POST | api/admin/elections | Admin | Create an election |
| (CRUD) | api/admin/elections/{id}/candidates | Admin | Manage candidates |

## Tech stack (pinned — do not drift)
ASP.NET Core 8.0, EF Core 8, SQL Server 2022, BCrypt.Net-Next 4, JWT Bearer,
SignalR 8, SendGrid v3, FluentValidation, xUnit + Moq. React 18.2, Vite 5,
Redux Toolkit 2.2, Axios 1.6, Tailwind CSS / Material UI, Recharts,
Jest + React Testing Library.

## Testing expectations
- Backend: xUnit + Moq. Unit-test services in isolation (mock repositories).
  Integration-test the auth → vote → tally chain.
- Frontend: Jest + React Testing Library for components and the voting flow.
- Reference UAT target from the report: full voting workflow under 5 minutes,
  unassisted.

## Definition of done for any feature
Plan approved → code (full methods) → migration shown if schema changed →
tests added/passing → no secrets committed → respects the dependency rule.
