# Phase 3 — Elections, Candidates, Voting + SignalR

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Election lifecycle (Upcoming → Active → Closed), admin election + candidate
CRUD, and the vote submission flow exactly as PLAN.md specifies: open a DB
transaction, check voting status, store SHA-256(UserId) as VoterId, write the
vote, update VoterElectionStatus, commit, then broadcast the new tally over
a SignalR `ResultsHub`, returning a confirmation hash. RBAC via
`[Authorize(Roles=...)]` throughout. No new entities — `Election`,
`Candidate`, `Vote`, `VoterElectionStatus` all exist from Phase 1. **No new
migration.**

## Election lifecycle: computed, not admin-toggled
CLAUDE.md's API table has no `activate`/`close` endpoint — only creation and
CRUD. I'm treating `Status` as **derived from `StartDate`/`EndDate`**, not
manually flipped by an admin: `Upcoming` if `now < StartDate`, `Active` if
`StartDate <= now < EndDate`, `Closed` if `now >= EndDate`. Every place that
reads an election (listing, ballot access, vote submission, results)
computes the effective status from the dates and lazily syncs the stored
`Status` column if it's stale, rather than trusting whatever's currently
persisted. This means correctness never depends on a background job or an
admin remembering to click something — it also means there's no way for an
admin to manually force-close an election early or extend it without editing
dates. **Flagging this as a decision** — say if you want explicit
activate/close endpoints instead.

## RBAC for election/candidate management
CLAUDE.md's table just says "Admin" for `api/admin/elections` and the
candidate CRUD, but the `UserRole` enum has a distinct `ElectionOfficer`
separate from `Administrator` — I'm reading that as: officers manage
elections day-to-day, administrators additionally manage user accounts.
So election creation and candidate CRUD will authorize **both**
`Administrator` and `ElectionOfficer`; user provisioning (Phase 2, already
built) stays `Administrator`-only. Concretely: `AdminController`'s
class-level `[Authorize]` broadens to `Roles = "Administrator,ElectionOfficer"`,
with an action-level override back to `Roles = "Administrator"` specifically
on `CreateUser` (action-level `[Authorize]` replaces class-level, it doesn't
union, so this correctly keeps user creation admin-only while opening the
rest of the controller to officers too). **Flagging this as a decision** —
say if you want `ElectionOfficer` excluded and both endpoint groups
Administrator-only.

## Phase 2 touch-up: generalize `Result<T>`'s error type
`Result<T>` (from Phase 2) is currently hardcoded to `AuthError`
(`DuplicateEmail`, `InvalidCredentials`, `ValidationFailed`). Phase 3 needs
new failure cases (`ElectionNotActive`, `InvalidCandidate`, `AlreadyVoted`,
`NotFound`) that have nothing to do with auth. Rather than inventing a
parallel `VoteResult<T>`/`VoteError` type, I'm renaming `AuthError` →
`AppError` in place and adding the new members, so the whole app shares one
`Result<T>` pattern. `AuthService`, `AuthController`, and `AdminController`
get their `AuthError` references renamed to `AppError` (mechanical, full
method rewrites per CLAUDE.md's rule, no behavior change).

## Vote submission — the core flow
`POST api/elections/{electionId}/vote`, `[Authorize(Roles = "Voter")]`
(strictly Voter — an Administrator/ElectionOfficer account has no voter
privileges under this contract; they'd need a separate voter account to
vote, matching the API table's literal "Voter" column).

1. Load the election; compute effective status; if not `Active` →
   `AppError.ElectionNotActive` (409).
2. Load the candidate; verify its `ElectionId` matches the route →
   `AppError.InvalidCandidate` (400) if not.
3. Check `VoterElectionStatus` for `(UserId, ElectionId)`; if `HasVoted` →
   `AppError.AlreadyVoted` (409) — **before** touching `Votes`, so the common
   case never even reaches the DB constraint.
4. Begin an explicit DB transaction (extending Phase 2's `IUnitOfWork` with
   `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` — a single
   `SaveChangesAsync()` covering both writes would already be atomic via EF
   Core's implicit transaction, but an explicit transaction matches PLAN.md's
   literal instruction and gives an explicit rollback point if the insert
   below throws).
5. Compute `VoterId = SHA-256(UserId)` hex-encoded via a new
   `IVoterAnonymizer` (Infrastructure-implemented, matching CLAUDE.md's
   "Infrastructure: ... hashing services" placement, same reasoning as
   Phase 2's `IPasswordHasher`).
6. Insert the `Vote` row (`ElectionId`, `VoterId` = the hash — **never the
   raw UserId**, `CandidateId`, `VotedAt`).
7. Upsert `VoterElectionStatus` (`HasVoted = true`, `VotedAt = now`) — insert
   if this is the voter's first interaction with this election, update if a
   row already exists.
8. `SaveChangesAsync()` + commit. If it throws on the `(VoterId, ElectionId)`
   unique constraint (a race — two concurrent requests both passed step 3),
   roll back and translate to the same `AppError.AlreadyVoted` — this is the
   DB-level backstop PLAN.md's acceptance criteria calls for, on top of the
   app-level check in step 3.
9. Compute the confirmation hash: `HMAC-SHA256(key = Voting:ConfirmationSecret,
   message = "{VoteId}:{ElectionId}")`, hex-encoded, stored as `Vote.VoteHash`
   — this **is** the "confirmation hash" from CLAUDE.md's security rules
   (verifiable by recomputing from VoteId + ElectionId + the secret; not
   reversible; deliberately excludes CandidateId so the receipt can't leak
   which candidate was chosen).
10. Recompute the full tally for the election (all candidates, including
    those with zero votes — the query starts from `Candidates`, not `Votes`,
    so nobody silently disappears from the chart) and broadcast it over
    `ResultsHub` to the `election-{electionId}` SignalR group.
11. Return `{ VoteId, ConfirmationHash, VotedAt }`.

## SignalR
`ResultsHub` (`EVoting.API/Hubs/ResultsHub.cs`) lives in API per CLAUDE.md's
architecture table ("EVoting.API — controllers, middleware, **SignalR
hubs**, DI wiring"). Anonymous access (results are public). One client
method: `JoinElection(Guid electionId)` — adds the caller's connection to
group `election-{electionId}`. Server → client push: `ReceiveResults`,
payload = the same tally shape the REST results endpoint returns.

Broadcasting is behind an `IResultsBroadcaster` interface defined in
`Application` (so `VoteService` doesn't take a direct dependency on SignalR
types) and implemented in `EVoting.API` using `IHubContext<ResultsHub>` —
this is the one place `Infrastructure` doesn't hold the implementation,
because the hub itself is API-layer per CLAUDE.md, and DI just registers
the API-layer implementation against the Application-layer interface,
same as any other composition-root wiring. No new NuGet package — SignalR's
server side ships in the ASP.NET Core shared framework; `@microsoft/signalr`
is already installed on the frontend from Phase 0.

## Other endpoints

**`GET api/elections`** ("eligible elections for the voter") — returns
**all** elections (Upcoming/Active/Closed), each annotated with `HasVoted`
(looked up via `VoterElectionStatus` for the calling voter). There's no
constituency/eligibility-list concept anywhere in the schema, so "eligible"
can't mean a filtered subset — this returns everything with enough context
(status + has-voted) for the frontend to decide what to surface prominently.
**Flagging this as a decision** — if you intended "eligible" to mean
"currently open for voting" (i.e. filter to `Active` only), say so and I'll
narrow it.

**`GET api/elections/{id}/ballot`** — `[Authorize(Roles = "Voter")]`. 404 if
the election doesn't exist, `AppError.ElectionNotActive` (409) if not
currently `Active`, `AppError.AlreadyVoted` (409) if this voter already has
`HasVoted = true` for it (blocks re-viewing the ballot once voted — pushes
the frontend to the results view instead). Otherwise returns election title/
description + candidate list (`CandidateId`, `Name`, `Party`, `PhotoUrl`).

**`GET api/elections/{id}/results`** — `[AllowAnonymous]`, public per the API
table. Same tally shape as the SignalR broadcast.

**`POST api/admin/elections`** — creates an `Election`, `CreatedBy` forced
server-side from the caller's JWT UserId (never client-supplied, same
trust pattern as Phase 2's Role-forcing on register). Validates
`EndDate > StartDate`.

**Candidate CRUD** under `api/admin/elections/{electionId}/candidates` — full
CRUD (`POST`, `GET` list, `PUT` update, `DELETE`). All four **reject with 409
once the election's computed status is no longer `Upcoming`** — you can't
add/edit/remove candidates once voting may have started, protecting
ballot fairness. (Deleting a candidate that already has votes would also
fail at the DB level regardless — `Vote.CandidateId` is `Restrict`, per
Phase 1 — this rule just gives a clean 409 instead of a raw DB error.)

## New files

**`EVoting.Application`**
- `DTOs/Elections/CreateElectionRequestDto.cs`, `ElectionResponseDto.cs`,
  `ElectionSummaryDto.cs` (list view, includes `HasVoted`)
- `DTOs/Elections/BallotResponseDto.cs`, `BallotCandidateDto.cs`
- `DTOs/Elections/CastVoteRequestDto.cs` (`CandidateId`), `CastVoteResponseDto.cs`
  (`VoteId`, `ConfirmationHash`, `VotedAt`)
- `DTOs/Elections/CandidateTallyDto.cs` (`CandidateId`, `Name`, `Party`,
  `VoteCount`), `ResultsResponseDto.cs` (`ElectionId`, `Title`, `Status`,
  `Tally`, `TotalVotes`)
- `DTOs/Candidates/CreateCandidateRequestDto.cs`,
  `UpdateCandidateRequestDto.cs`, `CandidateResponseDto.cs`
- `Common/Result.cs` — **rewritten**: `AuthError` → `AppError`, adds
  `ElectionNotActive`, `InvalidCandidate`, `AlreadyVoted`, `NotFound`
- `Interfaces/IElectionRepository.cs`, `ICandidateRepository.cs`,
  `IVoteRepository.cs` (includes `GetTallyAsync`),
  `IVoterElectionStatusRepository.cs`
- `Interfaces/IVoterAnonymizer.cs` (`ComputeVoterId(Guid userId)`),
  `IConfirmationHashService.cs` (`Compute(Guid voteId, Guid electionId)`)
- `Interfaces/IResultsBroadcaster.cs` (`BroadcastResultsAsync(Guid electionId,
  IEnumerable<CandidateTallyDto> tally)`)
- `Interfaces/IUnitOfWork.cs` — **rewritten**: adds
  `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`
- `Services/ElectionService.cs` — create election, list (with computed
  status + HasVoted), ballot, results; owns the "compute + lazily sync
  Status" logic
- `Services/CandidateService.cs` — CRUD, enforces the Upcoming-only lock
- `Services/VoteService.cs` — the vote flow above
- `Validators/CreateElectionRequestValidator.cs`,
  `CreateCandidateRequestValidator.cs`, `UpdateCandidateRequestValidator.cs`,
  `CastVoteRequestValidator.cs`
- `DependencyInjection.cs` — **rewritten**: registers `IElectionService`,
  `ICandidateService`, `IVoteService` alongside Phase 2's `IAuthService`

**`EVoting.Infrastructure`**
- `Persistence/Repositories/ElectionRepository.cs`, `CandidateRepository.cs`,
  `VoteRepository.cs` (tally query starts from `Candidates`, left-joins
  vote counts so zero-vote candidates still appear), `VoterElectionStatusRepository.cs`
- `Security/Sha256VoterAnonymizer.cs`, `ConfirmationHashService.cs` (HMAC-SHA256,
  reads `Voting:ConfirmationSecret` from configuration)
- `Persistence/UnitOfWork.cs` — **rewritten**: implements the new transaction
  methods via `AppDbContext.Database.BeginTransactionAsync()` etc.
- `DependencyInjection.cs` — **rewritten**: registers the new repositories +
  `IVoterAnonymizer` + `IConfirmationHashService`

**`EVoting.API`**
- `Hubs/ResultsHub.cs` — `JoinElection(Guid electionId)`, anonymous access
- `Hubs/ResultsBroadcaster.cs` — implements `IResultsBroadcaster` via
  `IHubContext<ResultsHub>`
- `Controllers/ElectionsController.cs` — `[Route("api/elections")]`: `GET /`
  (Voter), `GET /{id}/ballot` (Voter), `POST /{id}/vote` (Voter),
  `GET /{id}/results` (AllowAnonymous)
- `Controllers/AdminController.cs` — **rewritten**: class-level
  `[Authorize(Roles = "Administrator,ElectionOfficer")]`, `CreateUser` action
  overridden back to `[Authorize(Roles = "Administrator")]`; adds
  `POST elections`, `GET/POST elections/{id}/candidates`,
  `PUT/DELETE elections/{id}/candidates/{candidateId}`
- `Program.cs` — **full rewrite**: adds `builder.Services.AddSignalR()`,
  `app.MapHub<ResultsHub>("/hubs/results")`, registers `IResultsBroadcaster`,
  everything else from Phase 2 carries over unchanged

## Secrets
`Voting:ConfirmationSecret` joins `Jwt:Key` / `ConnectionStrings:Default` /
`SeedAdmin:*` as a user-secret. New empty placeholder in `appsettings.json`.

## Acceptance check (per PLAN.md)
- A second vote by the same voter in the same election is rejected by both
  the app check (`VoterElectionStatus.HasVoted`) and the DB constraint
  (`(VoterId, ElectionId)` unique index, exercised under a race).
- Tally broadcasts over SignalR on each vote.
- Ballot anonymisation verified — `Votes.VoterId` is always the SHA-256
  hash; the raw `UserId` is never written to `Votes`.

## Open questions for you before I write any code
1. Computed election status (from dates) vs. explicit admin activate/close
   endpoints — confirmed, or do you want manual control?
2. `Administrator` + `ElectionOfficer` both allowed to manage
   elections/candidates, `Administrator`-only for user provisioning — confirmed,
   or should `ElectionOfficer` be excluded from admin endpoints entirely?
3. `GET api/elections` returns all elections + `HasVoted` flag, not filtered
   to just `Active` — confirmed, or narrow it to currently-open elections only?
4. Ballot blocked (409) once a voter has already voted, rather than still
   viewable read-only — confirmed?
