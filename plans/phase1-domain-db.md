# Phase 1 — Domain Entities + EF Core AppDbContext + Initial Migration

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Implement the seven Domain entities exactly as specified in CLAUDE.md's data
model, an `AppDbContext` in Infrastructure that maps them with the constraints
CLAUDE.md calls out (especially the composite `UNIQUE (VoterId, ElectionId)`
on Votes), and generate — but not apply — the initial EF Core migration.

## Scope
- `EVoting.Domain`: 7 entity classes + 2 enums. No EF Core package reference
  in Domain (POCOs only — Domain has zero dependencies per CLAUDE.md).
- `EVoting.Infrastructure`: `AppDbContext`, one `IEntityTypeConfiguration<T>`
  per entity, a design-time `IDesignTimeDbContextFactory<AppDbContext>` (so
  `dotnet ef migrations add` works without a real connection string), and the
  `EntityFrameworkCore.SqlServer` + `EntityFrameworkCore.Design` package refs.
- `EVoting.API`: minimal `AddDbContext<AppDbContext>(...)` registration in
  `Program.cs`, reading the connection string from configuration (which comes
  from env var / user-secrets at runtime — `appsettings.json` keeps the
  existing empty placeholder).
- The migration command (for you to run) — migration is generated as files
  in this step's implementation, but **not applied to any database**.
- Out of scope: repositories, services, controllers, auth, anything from
  Phase 2+.

## Entities (`EVoting.Domain/Entities/`)

Plain POCO classes, public auto-properties, no EF Core attributes (mapping
lives entirely in Infrastructure's Fluent API configurations to keep Domain
dependency-free).

**User** (`Users` table)
| Property | Type | Notes |
|---|---|---|
| UserId | `Guid` | PK |
| FullName | `string` | required, max 200 |
| Email | `string` | required, max 256, unique index |
| PasswordHash | `string` | required, max 200 (BCrypt hash) |
| Role | `UserRole` (enum) | required |
| IsVerified | `bool` | default `false` |
| CreatedAt | `DateTime` | required, UTC |

**Election** (`Elections` table)
| Property | Type | Notes |
|---|---|---|
| ElectionId | `Guid` | PK |
| Title | `string` | required, max 200 |
| Description | `string` | required, max 2000 |
| StartDate | `DateTime` | required, UTC |
| EndDate | `DateTime` | required, UTC |
| Status | `ElectionStatus` (enum) | default `Upcoming` |
| CreatedBy | `Guid` | FK → User.UserId |

**Candidate** (`Candidates` table)
| Property | Type | Notes |
|---|---|---|
| CandidateId | `Guid` | PK |
| ElectionId | `Guid` | FK → Election |
| Name | `string` | required, max 150 |
| Party | `string` | required, max 100 |
| PhotoUrl | `string?` | optional, max 500 |

**Vote** (`Votes` table)
| Property | Type | Notes |
|---|---|---|
| VoteId | `Guid` | PK |
| ElectionId | `Guid` | FK → Election |
| VoterId | `string` | required, `NVARCHAR(64)` — SHA-256(UserId) hex, **no FK to User** (anonymised by design) |
| CandidateId | `Guid` | FK → Candidate |
| VotedAt | `DateTime` | required, UTC |
| VoteHash | `string` | required, `NVARCHAR(64)` |

`UNIQUE (VoterId, ElectionId)` — enforced as a unique index in
`OnModelCreating`, this is the DB-level one-vote-per-voter guarantee.

**VoterElectionStatus** (`VoterElectionStatuses` table)
| Property | Type | Notes |
|---|---|---|
| UserId | `Guid` | FK → User, part of composite PK |
| ElectionId | `Guid` | FK → Election, part of composite PK |
| HasVoted | `bool` | default `false` |
| VotedAt | `DateTime?` | set when `HasVoted` flips true |

Composite PK `(UserId, ElectionId)`. This table is keyed on the *real*
UserId (unlike Votes) because the vote handler needs to check "has this
specific logged-in user voted?" without touching the anonymised ballot —
that's the split CLAUDE.md's vote flow (Phase 3) describes.

**OtpToken** (`OtpTokens` table)
| Property | Type | Notes |
|---|---|---|
| OtpTokenId | `Guid` | PK |
| UserId | `Guid` | FK → User |
| OtpHash | `string` | required, max 256 (hashed OTP) |
| ExpiresAt | `DateTime` | required, UTC |
| IsUsed | `bool` | default `false` |
| CreatedAt | `DateTime` | required, UTC |

**AuditLog** (`AuditLogs` table)
| Property | Type | Notes |
|---|---|---|
| AuditLogId | `Guid` | PK |
| Timestamp | `DateTime` | required, UTC |
| UserId | `Guid?` | FK → User, **nullable** (system-initiated actions have no user) |
| Action | `string` | required, max 500, free-text description |

## Enums (`EVoting.Domain/Enums/`)
- `UserRole { Voter, ElectionOfficer, Administrator }`
- `ElectionStatus { Upcoming, Active, Closed }`

## AppDbContext (`EVoting.Infrastructure/Persistence/AppDbContext.cs`)
`DbSet<User> Users`, `DbSet<Election> Elections`, `DbSet<Candidate> Candidates`,
`DbSet<Vote> Votes`, `DbSet<VoterElectionStatus> VoterElectionStatuses`,
`DbSet<OtpToken> OtpTokens`, `DbSet<AuditLog> AuditLogs`.

`OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(...)`
and loads one `IEntityTypeConfiguration<T>` per entity from
`Persistence/Configurations/`, keeping constraint definitions next to each
entity's config rather than one large method.

## Foreign key delete-behavior plan (needs your sign-off)
SQL Server rejects multiple cascade paths to the same table. `Votes` is
reachable two ways from `Election` (`Election → Votes` directly, and
`Election → Candidates → Votes`), so both FKs on `Votes` must be
**`Restrict`** (no cascade) — deleting an election or candidate that has
votes will fail at the DB level unless the app explicitly clears votes
first. This matches reality: elections aren't meant to be hard-deleted once
they have ballots.

Proposed behavior per relationship:
| FK | On delete |
|---|---|
| Election.CreatedBy → User | Restrict |
| Candidate.ElectionId → Election | Cascade |
| Vote.ElectionId → Election | **Restrict** (avoids multi-cascade-path error) |
| Vote.CandidateId → Candidate | **Restrict** (same reason) |
| VoterElectionStatus.UserId → User | Cascade |
| VoterElectionStatus.ElectionId → Election | Restrict |
| OtpToken.UserId → User | Cascade |
| AuditLog.UserId → User | SetNull (UserId is nullable — keep audit history if a user record is ever removed) |

## Other design decisions (flagging for approval, not assuming)
1. **Enum storage:** propose storing `Role` and `Status` as strings via
   `.HasConversion<string>()` (readable rows for an audit-sensitive system)
   rather than raw `int`. Say if you'd rather keep them as `int`.
2. **GUID generation:** app-generated `Guid.NewGuid()` (set in each entity's
   constructor), not `NEWID()` DB defaults — keeps ID generation testable in
   the Domain layer without hitting the database. `CreatedAt`/`VotedAt`/
   `Timestamp` default to `DateTime.UtcNow`, also app-side.
3. **Entity style:** plain POCOs with public getters/setters (data-model
   style), not encapsulated entities with private setters and behavior
   methods (e.g. `Election.Activate()`). Simpler and matches this project's
   scale; flag if you want stronger invariant enforcement in the entities
   themselves instead of in Application-layer services (Phase 2/3).
4. **DbContext registration timing:** adding the minimal
   `AddDbContext<AppDbContext>(...)` call to `Program.cs` now (reading
   `ConnectionStrings:Default` from configuration) so the API project builds
   as a valid EF Core migrations startup project. This is wiring, not
   business logic, so I think it belongs in Phase 1 rather than waiting for
   Phase 2 — say if you'd rather defer it.

## NuGet packages to add
- `EVoting.Infrastructure`: `Microsoft.EntityFrameworkCore.SqlServer` (8.x)
- `EVoting.API`: `Microsoft.EntityFrameworkCore.Design` (8.x) — required by
  the `dotnet ef` CLI tooling on the startup project.

## Migration command (for you to run manually)
After the entities/config land and packages are installed, I will NOT run
this — I'll hand it to you:

```
dotnet ef migrations add InitialCreate `
  --project backend/src/EVoting.Infrastructure `
  --startup-project backend/src/EVoting.API `
  --output-dir Persistence/Migrations
```

This only generates migration files (`Persistence/Migrations/*.cs`) using the
design-time factory's dummy connection string — it does not touch a real
database. Applying it (`dotnet ef database update`) is a separate command you
run later, against your actual SQL Server instance, once
`ConnectionStrings:Default` is set via user-secrets or an env var.

## Acceptance check (per PLAN.md)
- Entity classes match the schema tables in CLAUDE.md field-for-field.
- The `(VoterId, ElectionId)` unique constraint is expressed in
  `OnModelCreating` (via the Vote entity configuration).
- Migration is generated (files exist under `Persistence/Migrations/`) but
  not applied — no `dotnet ef database update` run by me.

## Open questions for you before I write any code
1. OK with enums stored as strings in the DB (vs. `int`)?
2. OK with app-generated GUIDs/UTC timestamps (vs. DB-side `NEWID()`/`GETUTCDATE()`)?
3. OK with plain POCO entities (vs. encapsulated entities with behavior methods)?
4. OK wiring `AddDbContext` into `Program.cs` now in Phase 1 (vs. deferring to Phase 2)?
5. OK with the Restrict/Cascade/SetNull delete-behavior table above?
