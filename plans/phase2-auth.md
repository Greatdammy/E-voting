# Phase 2 — Auth Module (Register → Login → JWT)

Status: PROPOSED — awaiting approval. No source files created yet.

**Revision note:** the original version of this plan included an OTP
second factor. Per your direction, OTP has been removed entirely — login is
now a single-factor email+password check that issues the JWT directly. See
`plans/phase1-domain-db.md`'s amendment note: the `OtpToken` entity/config
added in Phase 1 has already been deleted as unused (committed separately).
`CLAUDE.md` and `PLAN.md` have been updated to match (API table, security
rules, top summary, Phase 2/5 prompts).

## Goal
Full authentication slice: register (voter self-signup), login (email +
password → JWT directly). Plus, per your decisions: audit logging of
register/login attempts, and admin user provisioning (seeded first admin +
an admin-only endpoint to create further Officer/Administrator accounts).
Matches CLAUDE.md's security rules (BCrypt-12, HMAC-SHA256 JWT with 8h
expiry carrying UserId + role, FluentValidation on every input, 10 req/min/IP
rate limit on auth endpoints).

No schema changes — `User` (from Phase 1) and `AuditLog` (from Phase 1)
already cover this phase's persistence needs. No new migration.

## Flow this implements
1. **Register** (`POST api/auth/register`, public) — creates a `User` with
   `Role` forced to `Voter` server-side (never taken from client input),
   BCrypt-12 password hash, `IsVerified = true` (no verification step exists
   anymore, so the field is set true at creation — see note below). Rejects
   duplicate emails (409). No token issued here. Logs an `AuditLogs` entry
   (`"User registered"`, UserId, success) — and a failure entry
   (`"Registration failed: duplicate email"`, UserId = null since the user
   doesn't exist yet) on the duplicate-email path.
2. **Login** (`POST api/auth/login`, public) — verifies email + password. On
   success: issues the JWT immediately (HMAC-SHA256, 8h, claims = UserId +
   Role) and logs an `AuditLogs` success entry. On failure: generic "invalid
   email or password" (same message whether the email doesn't exist or the
   password is wrong — no user-enumeration signal) and logs a failure entry
   (`UserId` = null if the email didn't match any account, otherwise the
   matched user's id, so failed-login patterns are traceable without leaking
   account existence in the HTTP response itself).
3. **Admin user creation** (`POST api/admin/users`, `[Authorize(Roles =
   "Administrator")]`) — lets an authenticated Administrator create a user
   with any role (Voter, ElectionOfficer, Administrator) — this is how
   Officer/Admin accounts get provisioned going forward. Same duplicate-email
   check and BCrypt hashing as register, but `Role` comes from the request
   body since the caller is already an authenticated admin (trusted context,
   unlike the public register endpoint). Logs an `AuditLogs` entry
   attributing the action to the creating admin's UserId.
4. **First-admin seed** — on startup, `AdminSeeder` checks whether any
   `Administrator` user exists; if not, creates one from
   `SeedAdmin:Email`/`SeedAdmin:Password` configuration (env var /
   user-secrets, never source — new placeholder keys alongside `Jwt:Key` /
   `ConnectionStrings:Default`). Idempotent (safe on every startup — does
   nothing once an admin exists), skips with a logged warning if the config
   values aren't set. This is a runtime data operation via EF Core, not a
   schema migration, so it doesn't conflict with the manual-migrations rule.

**Note on `IsVerified`:** with no verification step in this design, the field
has no gate left to control. Setting it `true` at creation keeps its meaning
intact ("account created and usable") rather than leaving it permanently
`false` and meaningless. If you'd rather drop the column entirely since
nothing reads it, say so — I left it in for now since removing a schema
field is a bigger change than just not using it, and it costs nothing to
keep.

## New files

**`EVoting.Application`**
- `DTOs/Auth/RegisterRequestDto.cs`, `RegisterResponseDto.cs`
- `DTOs/Auth/LoginRequestDto.cs`, `LoginResponseDto.cs` (Token, ExpiresAt,
  UserId, Role)
- `DTOs/Admin/CreateUserRequestDto.cs`, `CreateUserResponseDto.cs` (includes
  `Role` in the request, unlike public register)
- `Common/Result.cs` — lightweight success/failure wrapper with an
  `AuthError` enum (`DuplicateEmail`, `InvalidCredentials`,
  `ValidationFailed`) so the controller maps failures to HTTP status codes
  (409 / 401 / 400) without throwing exceptions for expected business
  failures. Exception-handling middleware for genuinely unexpected errors
  stays Phase 5 scope.
- `Interfaces/IAuthService.cs` — `RegisterAsync`, `LoginAsync`,
  `CreateUserAsync(dto, role)` (shared with the admin-provisioning path to
  avoid duplicating the hash/duplicate-check logic)
- `Interfaces/IUserRepository.cs`
- `Interfaces/IPasswordHasher.cs` — `Hash(string)`, `Verify(string, string)`
- `Interfaces/IJwtTokenService.cs` — `GenerateToken(Guid userId, UserRole role)`
- `Interfaces/IAuditLogService.cs` — `LogAsync(Guid? userId, string action)`
- `Interfaces/IUnitOfWork.cs` — `SaveChangesAsync()`, wraps AppDbContext so
  repositories stay thin and Phase 3's transactional vote flow has a
  consistent commit point to reuse
- `Services/AuthService.cs` — implements `IAuthService`
- `Validators/RegisterRequestValidator.cs` — FullName required (max 200);
  Email required, valid format, max 256; Password required, min 8 chars, at
  least one uppercase/lowercase/digit (as you confirmed)
- `Validators/LoginRequestValidator.cs` — Email required + valid format;
  Password required
- `Validators/CreateUserRequestValidator.cs` — same as register plus `Role`
  must be a defined `UserRole`
- `DependencyInjection.cs` — `AddApplication()` extension registering
  `IAuthService` and scanning the assembly for `IValidator<T>`s

**`EVoting.Infrastructure`**
- `Persistence/Repositories/UserRepository.cs`
- `Persistence/Repositories/AuditLogRepository.cs` (backs `IAuditLogService`,
  or `IAuditLogService` is implemented directly here — naming TBD in code,
  not architecturally significant)
- `Persistence/UnitOfWork.cs`
- `Persistence/Seed/AdminSeeder.cs` — the startup seed routine described above
- `Security/BCryptPasswordHasher.cs` — BCrypt.Net-Next, work factor 12
- `Security/JwtTokenService.cs` — reads `Jwt:Key/Issuer/Audience` from
  configuration, HMAC-SHA256, 8h expiry, `ClaimTypes.NameIdentifier` +
  `ClaimTypes.Role` claims (so `[Authorize(Roles=...)]` works natively) plus
  `jti`/`iat`
- `Configuration/JwtSettings.cs` — options class bound from config
- `DependencyInjection.cs` — `AddInfrastructure(IConfiguration)` extension:
  registers `AppDbContext` (replacing the raw `AddDbContext` call Phase 1 put
  directly in `Program.cs`), plus all services/repositories above

**`EVoting.API`**
- `Controllers/AuthController.cs` — `[Route("api/auth")]`,
  `[EnableRateLimiting("AuthPolicy")]` at class level, `[AllowAnonymous]` on
  both actions, thin — validates via injected `IValidator<T>`, calls
  `IAuthService`, maps `Result` to status codes
- `Controllers/AdminController.cs` — `[Route("api/admin")]`,
  `[Authorize(Roles = nameof(UserRole.Administrator))]` at class level;
  `POST /users` action for user creation (kept in its own controller rather
  than `AuthController` since it's RBAC-gated admin functionality, not a
  public auth action — also gives Phase 3's election/candidate admin CRUD a
  natural home to grow into later)
- `Program.cs` — **full rewrite**: adds `AddAuthentication().AddJwtBearer(...)`
  (validates issuer/audience/lifetime/signing key from config),
  `AddRateLimiter` with a named `"AuthPolicy"` fixed-window policy (10
  permits/minute, partitioned by client IP, `QueueLimit = 0` → 429 over
  limit), calls the new `AddApplication()` and `AddInfrastructure(Configuration)`
  extensions (replacing Phase 1's inline `AddDbContext` call), adds
  `app.UseAuthentication()` before `app.UseAuthorization()` (missing today),
  `app.UseRateLimiter()`, and runs `AdminSeeder` once against a DI scope
  after `app.Build()`. Full middleware-ordering audit (CORS, HSTS, etc.) is
  still Phase 5 scope; this phase only adds what auth itself needs, in a
  defensible order.

## NuGet packages to add
- `EVoting.Application`: `FluentValidation` (11.x) — validators only, no
  `FluentValidation.AspNetCore` (deprecated for MVC auto-integration;
  validators are invoked manually in the controller instead)
- `EVoting.Infrastructure`: `BCrypt.Net-Next` (4.x)
- `EVoting.API`: `Microsoft.AspNetCore.Authentication.JwtBearer` (8.x) —
  rate limiting needs no package, it's built into ASP.NET Core 8

No SendGrid package — its only use case was OTP delivery, which no longer
exists. `SendGrid v3` stays listed in CLAUDE.md's pinned tech stack (it may
still be useful for a future notification feature) but isn't installed or
wired until something actually needs it.

## Secrets (per CLAUDE.md — env vars / user-secrets, never source)
`Jwt:Key`, `SeedAdmin:Email`, `SeedAdmin:Password` join
`ConnectionStrings:Default` as user-secrets. `appsettings.json` gets new
empty placeholder keys for `SeedAdmin:Email`/`SeedAdmin:Password` (structural
placeholders only, same pattern as the existing `Jwt`/`ConnectionStrings`
entries).

## Acceptance check (per PLAN.md, updated)
- Registration rejects duplicate emails (409).
- A valid JWT is returned only after correct email + password.
- Audit log entries are written for register/login attempts (success and
  failure).
- The seeded admin can call `POST api/admin/users` to create further
  Officer/Administrator accounts.
- No secrets in source.

## Open question for you before I write any code
1. Confirmed no OTP, no SendGrid — anything else you want dropped or added
   now that the second factor is gone (e.g., should failed-login audit
   entries trigger any kind of lockout after N attempts, or is the 10/min/IP
   rate limit sufficient for this project's scope)? I'm assuming rate
   limiting alone is sufficient and not adding account lockout — flag if you
   want it.
