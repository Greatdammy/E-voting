# OTP Verification Before Voting

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Require voters to verify a one-time passcode (OTP), emailed to their
registered address, immediately before each vote is cast. This is a second
factor scoped to the **act of voting itself**, not to login — a stolen or
replayed JWT alone is no longer sufficient to cast a ballot; the attacker
would also need access to the voter's inbox at the moment of voting.

## Flow
1. Voter opens the ballot (`GET /elections/{id}/ballot`) — unchanged.
2. Voter selects a candidate, clicks **"Send verification code"** →
   `POST /elections/{id}/otp/request`. Backend re-checks the same eligibility
   rules `CastVoteAsync` already enforces (election `Active`, voter hasn't
   already voted), generates a 6-digit numeric code, hashes and stores it,
   emails it via SendGrid, and returns only `{ expiresAt, maskedEmail }` —
   **never the code itself**.
3. Voter enters the code and clicks **"Submit vote"** →
   `POST /elections/{id}/vote` with `{ candidateId, otpCode }`.
   `VoteService.CastVoteAsync` verifies the OTP (matches, unexpired, unused,
   under the attempt cap) as its first step, before the existing
   election/candidate/already-voted checks, and marks the code consumed
   **inside the same transaction** as the vote insert — so two concurrent
   requests can't both spend one valid code.

Per your answer, OTP scope is **per election vote**: a voter voting in three
elections verifies three separate codes, each tied to that specific
`(UserId, ElectionId)` pair.

## Data model
New entity `VoteOtp`:
- `VoteOtpId` (GUID PK)
- `UserId` (FK → Users)
- `ElectionId` (FK → Elections)
- `CodeHash` (NVARCHAR(64)) — SHA-256 of the code, never the plaintext code,
  mirroring how `VoterId` and `VoteHash` are already handled
- `ExpiresAt` (datetime2)
- `AttemptCount` (int, default 0)
- `IsUsed` (bit, default false)
- `CreatedAt` (datetime2, default UTC now)
- Index on `(UserId, ElectionId, IsUsed)` for fast lookup of the current
  active code

Policy constants (Application layer, one place to tune):
- 6 digits, cryptographically random (`RandomNumberGenerator`, not `Random`)
- 5-minute expiry
- Max 5 verify attempts per code — exceeding it kills the code and the voter
  must request a new one
- 60-second cooldown between requests for the same `(UserId, ElectionId)`
- Max 5 requests per voter per election per hour (abuse ceiling; also caps
  email volume)
- Requesting a new code invalidates the previous one — only the most
  recently issued code for a given `(UserId, ElectionId)` is ever valid

## Backend changes by layer

**Domain** (`EVoting.Domain`): `VoteOtp` entity.

**Application** (depends on Domain only):
- `IOtpCodeHasher` (interface — implementation in Infrastructure, mirrors
  `IVoterAnonymizer` / `Sha256VoterAnonymizer`)
- `IVoteOtpRepository` (interface — implementation in Infrastructure)
- `IEmailService` — `Task SendOtpEmailAsync(string toEmail, string code,
  TimeSpan validFor)`. Application only knows "send an OTP email", not
  SendGrid — keeps the dependency rule intact.
- `IOtpService` — `RequestOtpAsync(userId, electionId)` and
  `VerifyAndConsumeAsync(userId, electionId, code)`, called from
  `VoteService`, not directly from the controller, so verification and vote
  insertion share one transaction.
- DTOs: `RequestOtpResponseDto { ExpiresAt, MaskedEmail }`; extend
  `CastVoteRequestDto` with `OtpCode` (string) + a FluentValidation rule
  (exactly 6 digits).
- New `AppError` values: `OtpNotFound`, `OtpExpired`, `OtpInvalid`,
  `OtpAttemptsExceeded`, `OtpRequestCooldown`, `OtpRequestLimitExceeded`.

**Infrastructure** (depends on Application + Domain):
- `VoteOtpRepository` (EF Core) + `VoteOtpConfiguration` +
  `AppDbContext.VoteOtps` `DbSet`.
- `Sha256OtpHasher : IOtpCodeHasher` (Security/) — same pattern as
  `Sha256VoterAnonymizer`.
- `IEmailService` implementations, chosen in `Program.cs` DI at startup:
  - `SendGridEmailService` — used when `SendGrid:ApiKey` is configured.
  - `LoggingEmailService` — fallback used when it isn't; logs the OTP via
    `ILogger` instead of sending, so local dev/demo/UAT runs work without a
    live SendGrid account. (Per your answer — build both.)

**API** (depends on Application only):
- `ElectionsController`: new `POST /elections/{id}/otp/request`
  (`[Authorize(Roles = "Voter")]`, `[EnableRateLimiting("AuthPolicy")]` —
  reuses the existing 10/min/IP limiter since this is a sensitive,
  email-triggering endpoint).
- `POST /elections/{id}/vote`: request DTO gains `OtpCode`; controller
  action itself is otherwise unchanged — `VoteService` does the OTP check.
- `MapError` extended for the new `AppError` values (400 for
  invalid/expired/not-found, 429 for cooldown/limit-exceeded).

## Migration
A migration is required for the new `VoteOtps` table. Per the project rule,
I will generate (not run) the command for you:

```
dotnet ef migrations add AddVoteOtp --project EVoting.Infrastructure --startup-project EVoting.API
```

`appsettings.json` already has an empty `SendGrid: { ApiKey, FromEmail }`
placeholder from earlier scaffolding — no new config section needed, just
populate it (via env var / user-secrets, never committed) when you have a
key.

## Frontend changes
- `BallotPage.jsx` becomes a two-stage form:
  - **Stage 1** (unchanged candidate picker) gains a **"Send verification
    code"** button in place of the current direct submit. On success, reveal
    Stage 2 and show the masked email address.
  - **Stage 2**: 6-digit code entry (a small reusable `OtpInput` component
    under `components/ui/`), a countdown to `expiresAt`, a "Resend code"
    action (disabled during the 60s cooldown), and the actual "Submit vote"
    button, which now posts `{ candidateId, otpCode }`.
  - Error states: invalid code (shows remaining attempts), expired/attempts
    exceeded (prompts resend), 429 (cooldown/limit messaging).
- No new axios instance needed — reuses `axiosInstance`.

## Security notes
- The code is never returned in any API response and never logged in
  plaintext; audit log entries read "OTP requested" / "OTP verified, vote
  cast" without the code itself.
- Hash comparison uses `CryptographicOperations.FixedTimeEquals` to avoid
  timing side-channels, even though attempt-limiting is the primary defense
  for a 6-digit space.
- Two-layer rate limiting: IP-based (`AuthPolicy`, 10/min) on the request
  endpoint, plus the DB-backed per-voter/election cooldown and hourly cap —
  stops both anonymous hammering and an authenticated voter spamming their
  own inbox or brute-forcing attempts.

## Testing plan
- **Backend** (xUnit + Moq): `OtpService` unit tests — code generation,
  hashing, expiry, attempt-cap, cooldown, and supersede-on-reissue logic.
  `VoteService` tests extended to reject a vote with a missing/wrong/expired/
  already-used OTP, and to accept + consume a valid one. Integration test
  extends the existing auth → vote → tally chain: seed a real `User` row
  (per the actor-id lesson already in project memory) → login → request OTP
  → read the code back via the `LoggingEmailService` test double → verify +
  vote → assert the tally updates.
- **Frontend** (Vitest + RTL): `BallotPage.test.jsx` new cases for
  request-code success/failure, code-verification success/failure, and
  resend-cooldown UI state.

## Explicit out of scope
- No SMS/phone OTP — the data model has no phone number field; email via
  SendGrid only, matching the pinned stack.
- No OTP requirement at login — this sits strictly between ballot selection
  and vote submission, per the per-vote scope you chose.
- No change to the one-vote-per-voter DB constraint or ballot anonymisation
  — `VoteOtp` rows reference the real `UserId` (needed to email the voter)
  but are never linked to the anonymised `Vote` row, so they don't weaken
  ballot secrecy.