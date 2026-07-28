# AI Election Integrity Guard (Anomaly Detection)

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
A backend-driven, admin/officer-facing **anomaly detection system** that watches
live voting activity for statistical signatures of automated or coordinated
voting (bot scripts, ballot-stuffing attempts, compromised endpoints) and
surfaces them as a real-time alert feed. This is the "notable AI feature" for
the project report: it is genuinely novel (uses a real ML time-series
detector, not decoration), and it reinforces the system's core premise —
election integrity — rather than being cosmetic.

## Why this, and why it's legitimate
The only signal the system has for a vote is `VotedAt` (timestamp),
`ElectionId`, `CandidateId`, and `VoterId` (already a SHA-256 hash — no raw
identity). Anything IP-based, device-fingerprint-based, or geolocation-based
would either not exist in the current schema or would conflict with the
anonymisation design (`CLAUDE.md` explicitly requires `VoterId` to be a hash,
never reversible). So the detector is deliberately scoped to **timing and
volume statistics only** — this is honest about what the data can support,
and it's still a real, useful signal: human voters take measurable time to
load a ballot, read it, and submit, so a burst of near-simultaneous votes or
a sustained velocity spike is a legitimate tell.

## Critical scope boundary
This is a **triage tool for human review, never an automated enforcement
mechanism**. It must never auto-reject, auto-flag-and-block, or alter a vote.
Alerts are advisory only, reviewed and dismissed/escalated by an Officer or
Admin. This preserves the append-only, tamper-evident nature of the ballot
box and avoids a false positive ever silently disenfranchising a real voter.
UI copy must say so explicitly ("Flagged for review — not an automatic
action") everywhere an alert appears, mirroring the existing turnout
forecast's "estimate only" disclaimer pattern.

## Architecture (respects the Clean Architecture dependency rule)

**Domain** (`EVoting.Domain`, no dependencies):
- `IntegrityAlert` entity: `AlertId` (GUID PK), `ElectionId` (FK), `AlertType`
  enum (`VelocitySpike`, `TimingCluster`), `Severity` enum (`Info`, `Warning`,
  `Critical`), `DetectedAtUtc`, `WindowStartUtc`, `WindowEndUtc`,
  `ObservedValue` (decimal — e.g. votes/minute), `BaselineValue` (decimal),
  `Status` enum (`Open`, `Reviewed`, `Dismissed`), `ReviewedBy` (nullable FK
  Users), `ReviewedAtUtc` (nullable).

**Application** (depends on Domain only):
- `IIntegrityMonitoringService` — `Task<IReadOnlyList<IntegrityAlert>>
  DetectAnomaliesAsync(Guid electionId)`, called by the background service.
- `IIntegrityAlertRepository` (or reuse a generic repository pattern already
  in the codebase) for persistence + query.
- DTOs: `IntegrityAlertDto`, `IntegritySummaryDto` (open/reviewed/dismissed
  counts per election).
- No new FluentValidation validators needed — alerts are system-generated,
  not user input. The one user input (resolve/dismiss action) gets a small
  validator for the request body (`AlertId`, optional `Note`).

**Infrastructure** (depends on Application + Domain):
- `Microsoft.ML.TimeSeries` (new NuGet package — **flagged for your
  approval**, see below) provides `DetectIidSpike` / `DetectSpikeBySsa`, ML.NET
  transforms purpose-built for spike detection in a time series. This is the
  "AI/ML" substance behind the feature, not a cosmetic label.
- `IntegrityMonitoringService` implementation:
  - Reads recent `Votes` rows for an `Active` election (last N minutes,
    windowed).
  - Buckets vote counts into fixed time buckets (e.g. 10-second buckets) to
    build a time series.
  - Runs `DetectIidSpike` over the bucketed series to flag statistically
    anomalous spikes relative to the recent baseline.
  - Separately computes inter-arrival times between consecutive votes;
    flags a `TimingCluster` when a run of votes lands within an
    implausibly tight window (e.g. more than K votes with sub-2-second
    gaps) — this needs no ML.NET, it's a simple threshold check, kept
    alongside the ML-based spike detector rather than replacing it.
  - Persists new `IntegrityAlert` rows (dedupes against already-open alerts
    for the same window so it doesn't spam).
  - Requires **≥30 bucketed data points** before running detection — same
    "don't overclaim on thin data" discipline as the turnout forecast;
    returns no alerts (not false ones) below that threshold.
- `IntegrityMonitorBackgroundService : BackgroundService` — polls every 15–30
  seconds for each `Active` election and calls `DetectAnomaliesAsync`. Kept
  entirely out of the vote-cast transaction path in `VoteService` — integrity
  detection must never add latency or risk to the security-critical vote
  write.
- EF Core: new `IntegrityAlerts` table, configured in
  `AppDbContext.OnModelCreating` (FK to `Elections`, FK to `Users` for
  `ReviewedBy`). **Migration will be generated for you to run manually — not
  applied automatically**, per the project rule.

**API** (`EVoting.API`, depends on Application only):
- `IntegrityController` (`[Authorize(Roles = "Administrator,ElectionOfficer")]`):
  - `GET api/admin/elections/{id}/integrity-alerts` — list (open by default,
    filterable by status).
  - `GET api/admin/elections/{id}/integrity-summary` — counts for a dashboard
    header.
  - `POST api/admin/elections/{id}/integrity-alerts/{alertId}/review` — body
    `{ status: "Reviewed" | "Dismissed", note?: string }`.
- SignalR: extend the existing `ResultsHub` (or add a narrowly-scoped
  `IntegrityHub`) with a `ReceiveIntegrityAlert` message, broadcast to a
  role-restricted group (Admin/Officer only) whenever a new alert is
  persisted — mirrors the existing "tally push" pattern for consistency.

## New backend dependency (flagging per your "no drift" pinned stack rule)
`Microsoft.ML` + `Microsoft.ML.TimeSeries` NuGet packages. This is new — the
pinned stack in CLAUDE.md doesn't list it. Rationale: it's Microsoft's own,
well-documented, self-hosted ML library (no external API, no new secret, no
network dependency) and it's the standard tool for exactly this problem
(IID spike / change-point detection over a time series). If you'd rather stay
dependency-free, the fallback is a hand-rolled z-score/EWMA spike detector in
plain C# (same detection logic, less "real ML" but zero new dependencies) —
your call, flag in your review.

## Frontend
- New Admin page `frontend/src/pages/admin/AdminIntegrityPage.jsx`:
  - Risk feed: list of alerts, severity badge (Info=sky, Warning=amber,
    Critical=rose — consistent with existing status-badge palette), window
    time range, observed vs. baseline value, Review/Dismiss actions.
  - A compact `recharts` sparkline of votes-per-bucket with anomalous
    buckets highlighted in rose, echoing the `TurnoutForecastCard`'s
    observed/projected line pattern.
  - Live-updates via the new SignalR channel (same `resultsConnection.js`
    pattern, or a small sibling `integrityConnection.js`).
  - Caption: "Flagged for review — not an automatic action. A human review
    step decides what happens next."
- `NavBar.jsx`: new link for Admin/Officer roles → `/admin/integrity`, icon
  `ShieldAlert` (lucide-react, already a dependency).
- `ResultsPage.jsx` (voter/public-facing): small, understated trust badge —
  "Monitored by an automated integrity check" — linking only for
  Admin/Officer viewers to the dashboard; no alert detail exposed publicly
  (an attacker shouldn't learn what triggers detection).

## Tests
- Backend (xUnit + Moq, matching CLAUDE.md's testing section): unit tests for
  `IntegrityMonitoringService` fed synthetic bucketed time series with known
  spikes — assert alerts raised at expected windows and suppressed below the
  30-point threshold. Integration test: seed a burst of votes in a test
  election, run detection, assert an alert is persisted and retrievable via
  the endpoint only for Admin/Officer roles (403 for Voter).
- Frontend (Vitest + RTL): `AdminIntegrityPage.test.jsx` — renders alert list,
  review/dismiss action calls the right endpoint, severity badge mapping.

## Explicit out of scope
- No IP address or device fingerprinting — would conflict with the
  anonymisation design and isn't needed for the timing/volume signal.
- No automatic vote invalidation, blocking, or rate limiting triggered by an
  alert — human review only.
- No cross-election baseline (first election run has no historical baseline
  to compare "off-hours" patterns against) — deferred; the velocity-spike and
  timing-cluster detectors work within a single election's own live data.
