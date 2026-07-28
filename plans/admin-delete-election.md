# Admin: Delete an Election

Status: IMPLEMENTED (2026-07-28).

## Context
The admin wants to remove elections they no longer need — either because an
election has expired (Closed) and its record is just clutter, or because
they created one by mistake and want it gone regardless of status. There was
no delete endpoint or UI for elections (only for candidates).

This had to be designed carefully because the whole system's premise is
election integrity: a delete action must never be able to silently destroy
real cast-vote data. Conveniently, the schema already has a safety net for
this — `Vote.ElectionId` and `VoterElectionStatus.ElectionId` are configured
`OnDelete(DeleteBehavior.Restrict)` against `Elections` (see
`VoteConfiguration.cs` / `VoterElectionStatusConfiguration.cs`), specifically
so a DB-level FK violation blocks deleting an election that has votes. The
implementation adds an Application-layer check that produces a clean 409
instead of letting a raw FK exception bubble up, but the underlying
guarantee was already there.

A frontend-only "hide from my admin view" alternative (localStorage-based,
like the turnout forecast) was considered and explicitly rejected: it would
only hide the election in the admin's own browser while it stayed fully
live for voters — a correctness risk (an admin believing something is
"deleted" while it's still votable), not real deletion.

## Decisions

**Who can delete: Administrator only.** The action carries
`[Authorize(Roles = nameof(UserRole.Administrator))]` layered on top of
`AdminController`'s class-level `[Authorize(Roles = "Administrator,ElectionOfficer")]`
— same pattern already used for `CreateUser`. Deleting an election is more
destructive than the candidate-management actions `ElectionOfficer` already
has, and the user asked for this "as an admin" specifically.

**When deletion is allowed: any status, gated only by zero votes cast.**
This satisfies both halves of the request — "expired" (a Closed election
with historical data they don't want) and "don't need it anymore" (an
Upcoming/Active one created by mistake) — while the real safety net is "has
it recorded any actual votes," which is the only thing that genuinely can't
be destroyed. Verified invariant: `VoterElectionStatus` rows are only ever
created inside `VoteService.CastVoteAsync` at the same moment a `Vote` row
is inserted — so an election has zero `VoterElectionStatus` rows whenever it
has zero `Vote` rows, meaning a single "any votes?" check is sufficient.

Named tradeoff (not silently decided): an Active election with zero votes
so far could technically be deleted while a voter is mid-session on its
ballot page — the ballot GET might already be cached client-side, but the
subsequent vote-cast POST would then 404 instead of succeeding. Accepted as
a narrow race, since it requires zero votes to even be possible.

**Audit logging:** yes, via the existing `IAuditLogService.LogAsync`,
matching `AuthService`'s convention.

## What shipped

**Backend:**
- `AppError.ElectionHasVotes` added to `Result.cs`, mapped to `Conflict` (409) in `AdminController.MapError`.
- `IVoteRepository.HasVotesAsync(electionId)` / `VoteRepository` impl (`AnyAsync`).
- `IElectionRepository.Remove(election)` / `ElectionRepository` impl (mirrors `ICandidateRepository.Remove`).
- `IElectionService.DeleteElectionAsync(electionId, deletedBy)` / `ElectionService` impl — fetch → NotFound check → vote-existence guard → `Remove` → audit log → `SaveChangesAsync`. `ElectionService` gained a new `IAuditLogService` constructor dependency (already registered in Infrastructure DI).
- `AdminController.DeleteElection` — `DELETE api/admin/elections/{electionId}`, Administrator-only.
- Candidates and IntegrityAlerts under the election cascade-delete automatically per existing FK config — no explicit cleanup needed.
- No EF Core migration required (no schema change).

**Frontend (`AdminElectionsPage.jsx`):**
- `handleDelete` mirrors `AdminCandidatesPage.jsx`'s pattern — `window.confirm` guard, `axiosInstance.delete`, reuses the page's existing `error` state for failures (e.g. the 409 "has votes cast" message surfaces directly, no new client-side vote-count pre-check).
- Delete button (ghost/rose `Trash2`, `aria-label="Delete"`) added to each election card's action group, **shown only when `role === 'Administrator'`** — a small addition beyond the original plan text, so `ElectionOfficer` admins (who share this page) never see a button that would only 403.

## Tests
- `backend/tests/EVoting.UnitTests/Services/ElectionServiceTests.cs` (new) — 3 facts: not-found, has-votes-guard (asserts `Remove`/`SaveChangesAsync` never called), success (asserts `Remove`/`LogAsync`/`SaveChangesAsync` each called once).
- `backend/tests/EVoting.IntegrationTests/ElectionDeletionTests.cs` (new) — 3 facts: 204 + row actually gone for Administrator with no votes; 403 for ElectionOfficer; 409 + row still present when a real `Vote` row exists (seeded directly, not via `VoteService`, to isolate the guard).
  - Bug caught and fixed during this pass, not in the app: the test's JWT must correspond to a real seeded `Users` row, not a bare `Guid.NewGuid()` — the success path's `IAuditLogService.LogAsync` call inserts an `AuditLog` row with a `UserId` FK, and SQLite (used by `CustomWebApplicationFactory`) enforces it. Fixed by generating the token for the seeded admin's actual `UserId`. Note: `IntegrityAlertsRbacTests.cs`'s `GenerateToken` has this same latent gap, just never triggered because none of its tests reach a full successful-review write — worth knowing if a "review succeeds" test is added there later.

## Verification
- `dotnet build`: clean, 0 warnings, 0 errors.
- `dotnet test`: 33/33 passing (24 unit, 9 integration — up from 27/27 before this change).
- `npm test`: 20/20 passing. `npm run build`: succeeds.
- Manual click-through not yet performed by either party this session — recommended before considering this fully done in practice.
