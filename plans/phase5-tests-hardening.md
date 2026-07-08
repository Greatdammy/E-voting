# Phase 5 — Tests + Hardening

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Per PLAN.md's literal scope: xUnit + Moq unit tests for `AuthService` and
`VoteService` (mock repositories); one integration test covering
register → login → vote → results; Jest + React Testing Library tests for
the login form and the voting flow; HSTS + HTTPS redirection middleware;
confirm CORS/auth/authorization/rate-limit ordering. I'm not expanding
coverage to `ElectionService`/`CandidateService` or additional frontend
pages beyond what's named — that would be scope creep past what PLAN.md
asks for this phase.

## Backend unit tests (`EVoting.UnitTests`)
Removes the `dotnet new xunit` template's placeholder `UnitTest1.cs`.
Adds `Moq` (4.20.x) — the only new package needed; `xunit`,
`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `coverlet.collector`
are already there from Phase 0, and the project already references
`Application`/`Infrastructure`/`Domain` from Phase 1.

**`Services/AuthServiceTests.cs`** — mocks `IUserRepository`,
`IPasswordHasher`, `IJwtTokenService`, `IAuditLogService`, `IUnitOfWork`:
- `RegisterAsync`: succeeds and forces `Role = Voter` regardless of
  anything the caller could try to pass; fails with `AppError.DuplicateEmail`
  when the email already exists.
- `LoginAsync`: succeeds and returns a JWT on correct credentials; fails
  with `AppError.InvalidCredentials` both when the email doesn't exist and
  when the password is wrong (same error either way — this is the
  **invalid-credentials case** the Phase 5 acceptance criterion calls out).
- `CreateUserAsync` (admin path): succeeds with the caller-supplied role;
  fails with `AppError.DuplicateEmail` on collision.

**`Services/VoteServiceTests.cs`** — mocks `IElectionRepository`,
`ICandidateRepository`, `IVoteRepository`, `IVoterElectionStatusRepository`,
`IVoterAnonymizer`, `IConfirmationHashService`, `IResultsBroadcaster`,
`IUnitOfWork`:
- Happy path: election active, candidate valid, voter hasn't voted → vote
  persisted, `VoterElectionStatus` upserted, confirmation hash computed,
  `IResultsBroadcaster.BroadcastResultsAsync` invoked (verified via Moq),
  success DTO returned.
- Election not found → `AppError.NotFound`.
- Election not `Active` (`Upcoming` or `Closed`) → `AppError.ElectionNotActive`.
- Candidate belongs to a different election → `AppError.InvalidCandidate`.
- **App-level double-vote**: `VoterElectionStatus.HasVoted == true` →
  `AppError.AlreadyVoted`, and the mocked `IVoteRepository.AddAsync` is
  verified as *never called* — confirms the check happens before any write
  is attempted.
- **DB-level double-vote (the race backstop)**: mock
  `IUnitOfWork.SaveChangesAsync()` to throw `UniqueConstraintViolationException`
  → `VoteService` catches it, calls `RollbackAsync()` (verified), and
  returns `AppError.AlreadyVoted`. This is how the DB-constraint path from
  PLAN.md's acceptance criterion gets exercised — simulating a real
  concurrent race in a test would need actual concurrent HTTP requests
  against a real timing window, which isn't worth the flakiness for this
  project; testing that `VoteService` correctly *reacts* to the exception
  Infrastructure would throw is the right level of isolation for a unit
  test.

Together, "double-vote... rejected by both the app check and the DB
constraint" (PLAN.md's acceptance line) is covered by these two
`VoteServiceTests` cases, and "invalid-credentials" by `AuthServiceTests`.

## Backend integration test (`EVoting.IntegrationTests`)
Removes the template `UnitTest1.cs`. Adds `Microsoft.AspNetCore.Mvc.Testing`
and `Microsoft.EntityFrameworkCore.Sqlite` (8.0.x, matching the EF Core
pin) — test-only dependencies, not added to `Infrastructure`.

**Why SQLite in-memory, not EF Core's InMemory provider:** `VoteService`
calls `IUnitOfWork.BeginTransactionAsync()`, which EF Core's InMemory
provider doesn't support at all (it's not a relational provider —
`Database.BeginTransactionAsync()` throws `InvalidOperationException`
against it). SQLite in-memory *is* relational — transactions and the
`(VoterId, ElectionId)` unique index both work correctly against it, so the
integration test exercises real constraint behavior, not a stand-in.

**`CustomWebApplicationFactory : WebApplicationFactory<Program>`** — opens
one `SqliteConnection("DataSource=:memory:")` for the factory's lifetime
(closing it destroys the in-memory DB, so it's held open until `Dispose`),
overrides the `AppDbContext` registration to `UseSqlite(connection)` instead
of the real `ConnectionStrings:Default` SQL Server one, calls
`Database.EnsureCreated()` (builds the schema straight from the current EF
Core model/Fluent API config — not from migration files, so this works
regardless of whether `InitialCreate` has been applied anywhere), and
supplies test values for `Jwt:Key`/`Issuer`/`Audience` and
`Voting:ConfirmationSecret` via in-memory configuration (`SeedAdmin:Email`/
`Password` are left blank so `AdminSeeder` just skips with a logged warning
— this test doesn't need a pre-seeded admin).

Requires one addition to `Program.cs`: `public partial class Program { }`
at the end of the file — the standard, behavior-neutral marker that makes
the implicit top-level-statements `Program` class visible to
`WebApplicationFactory<Program>` in the test assembly.

**`AuthVoteFlowTests.cs`** (`IClassFixture<CustomWebApplicationFactory>`):
1. Seed an `Active` election + one candidate directly via the factory's
   `AppDbContext` (there's no API path for a bare Voter to create an
   election — this is expected, admin-only by design; seeding
   prerequisite data directly is the standard integration-test pattern for
   this situation).
2. `POST /api/auth/register` → 201.
3. `POST /api/auth/login` → 200, extract the JWT.
4. `POST /api/elections/{id}/vote` with the JWT → 200, extract
   `VoteId`/`ConfirmationHash`.
5. `GET /api/elections/{id}/results` (no auth — public) → 200, tally shows
   1 vote for the seeded candidate.
6. Vote again with the same token → 409 (`AlreadyVoted`) — closes the loop
   on double-vote rejection through the *real* HTTP pipeline (routing, auth,
   validation, service, DB), not just at the unit level.

## Frontend tests — Vitest (decided)
CLAUDE.md pinned Jest, but per your decision this phase switches the
pinned frontend test runner to **Vitest** — Vite's own Jest-API-compatible
runner (same `describe`/`it`/`expect`, React Testing Library works
identically), configured via `vite.config.js`'s `test` field, reusing the
app's existing transform pipeline with no extra babel plugins needed for
`import.meta.env`/JSX. `CLAUDE.md`'s tech stack line updates from
"Jest + React Testing Library" to "Vitest + React Testing Library" as part
of this phase, alongside the actual test files, so the doc and the
implementation land together.

New devDependencies: `vitest`, `@testing-library/react`,
`@testing-library/jest-dom`, `jsdom`. `vite.config.js` gets a `test` block
(`environment: 'jsdom'`, a setup file registering `@testing-library/jest-dom`
matchers). `package.json` gets a `"test": "vitest run"` script.

**`pages/LoginPage.test.jsx`** — mocks `../api/axiosInstance`, renders
inside a test `Provider`/`MemoryRouter`: submitting valid credentials calls
`POST /auth/login` with the right payload and dispatches `setCredentials`;
a rejected call renders the error message; the "registration successful"
banner shows when navigated to with `location.state.registered`.

**`pages/BallotPage.test.jsx`** — mocks `axiosInstance`: renders the
fetched ballot's candidates, submitting without a selection shows a
validation message and doesn't call the API, selecting a candidate and
submitting calls `POST /elections/:id/vote` and replaces the form with the
confirmation panel (`VoteId`/`ConfirmationHash` visible).

## HSTS + HTTPS + middleware ordering
**Adds** (nothing currently present):
- `builder.Services.AddHsts(options => { options.MaxAge =
  TimeSpan.FromDays(365); options.IncludeSubDomains = true; })` — CLAUDE.md
  specifies 1-year max-age explicitly, longer than ASP.NET Core's 30-day
  default.
- `app.UseHsts()` in a `!Environment.IsDevelopment()` branch (mirrors the
  existing `IsDevelopment()` Swagger branch) — HSTS is meaningless and
  actively annoying against a local self-signed dev cert, matching
  Microsoft's own template convention.
- `builder.WebHost.ConfigureKestrel(o => o.ConfigureHttpsDefaults(h =>
  h.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13))` — enforces
  CLAUDE.md's "TLS 1.2+" at the Kestrel level, not just documented intent.

**Confirms, doesn't change** — I audited the pipeline order built up
across Phases 2–4 against Microsoft's documented ordering guidance and it's
already correct:
```
(prod) UseHsts()
UseHttpsRedirection()
UseCors("Frontend")
UseRateLimiter()
UseAuthentication()
UseAuthorization()
MapControllers()
MapHub<ResultsHub>()
```
CORS before the rate limiter, both before Authentication, Authentication
before Authorization — all match the documented order. Nothing to
reorder; I'm adding a short comment above this block explaining *why* the
order matters (a hidden constraint — reordering this without knowing the
rationale is an easy mistake for a future editor to make), not changing
the sequence itself.

**Explicitly not doing**: adding global exception-handling middleware.
Neither PLAN.md's Phase 5 prompt nor CLAUDE.md's security rules call for
it, so I'm treating it as out of scope here rather than quietly expanding
the phase — flag if you want it added.

## New/changed files
- `backend/tests/EVoting.UnitTests/Services/AuthServiceTests.cs`,
  `VoteServiceTests.cs` (new); delete `UnitTest1.cs`
- `backend/tests/EVoting.IntegrationTests/CustomWebApplicationFactory.cs`,
  `AuthVoteFlowTests.cs` (new); delete `UnitTest1.cs`
- `backend/src/EVoting.API/Program.cs` — full rewrite (HSTS, Kestrel TLS
  floor, ordering comment, `public partial class Program`)
- Frontend: `pages/LoginPage.test.jsx`, `pages/BallotPage.test.jsx`, plus
  whichever test-runner config the Jest-vs-Vitest decision produces

## NuGet / npm packages
- `EVoting.UnitTests`: `Moq` 4.20.x
- `EVoting.IntegrationTests`: `Microsoft.AspNetCore.Mvc.Testing` 8.0.x,
  `Microsoft.EntityFrameworkCore.Sqlite` 8.0.x
- Frontend: `vitest`, `@testing-library/react`, `@testing-library/jest-dom`,
  `jsdom` (devDependencies)

## Acceptance check (per PLAN.md)
- Tests pass.
- Double-vote is covered at both the app-check and DB-constraint layers
  (unit tests), and end-to-end through the real HTTP pipeline (integration
  test).
- Invalid-credentials is covered (`AuthServiceTests`).
- Middleware order matches the documented security design (confirmed
  correct; HSTS/TLS gaps closed).

