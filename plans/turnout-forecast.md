# Live Turnout Forecast (frontend-only)

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Add an AI/ML-flavoured feature to the results view — a **live turnout
forecast** that predicts the final vote total for an *Active* election
before it closes. Computed and stored entirely client-side: no backend
endpoint, no new API contract, no schema change. Uses only data the
frontend already has access to (`GET /elections` for `startDate`/`endDate`,
`GET /elections/{id}/results` + the `ResultsHub` SignalR stream for
`totalVotes`).

Per your two decisions: the model is a **hand-rolled least-squares linear
regression** (no new npm dependency — stays inside the pinned frontend
stack), fit over a **localStorage-persisted history** of snapshots per
election so the forecast has more than one page-load's worth of data to
work with.

## Why this is legitimately frontend-only
`ResultsPage` is already public (`GET .../results` has no role
restriction) and already receives a live stream of `totalVotes` via
`ReceiveResults`. Nothing here needs data the API doesn't already expose —
it's a client-side model fit over client-observed snapshots, not a proxy
for a hidden backend feature.

## Important scope boundary
The forecast is an **estimate for display only**. It must never be
presented as, or confused with, the official tally — this project's whole
premise is election-integrity, so the UI copy has to make the distinction
explicit ("Estimated — not the official count") every place the number
appears.

## New files

**`frontend/src/ml/linearRegression.js`**
Pure math, no dependencies:
- `fitLine(points)` — `points: [{x, y}]` → `{slope, intercept}` via
  ordinary least squares (`x` = timestamp in ms, normalized by subtracting
  the first point's `x` to keep numbers small and avoid float precision
  loss).
- `predict(line, x)` → `y` at a given `x`.

**`frontend/src/utils/turnoutHistory.js`**
localStorage persistence, keyed `evoting_turnout_{electionId}`:
- `appendSnapshot(electionId, totalVotes)` — pushes `{t: Date.now(), v:
  totalVotes}` only if `totalVotes` changed since the last stored point
  (dedupe — SignalR/poll may fire with no actual change). Caps each
  election's array at 300 points (drops oldest on overflow) so a
  long-running election can't grow localStorage unbounded.
- `getHistory(electionId)` → the stored array, or `[]`.
- `pruneStaleElections(activeElectionIds)` — removes any
  `evoting_turnout_*` key whose election id isn't in the list passed in.
  Called from `ElectionsPage` on mount (it already fetches the full
  election list) so history for elections that have closed and scrolled
  out of view eventually gets garbage-collected instead of living in
  localStorage forever.

**`frontend/src/hooks/useTurnoutForecast.js`**
`useTurnoutForecast({ electionId, totalVotes, status, endDate })`:
- Appends a snapshot (via `turnoutHistory`) whenever `totalVotes` changes.
- Reads back the full history, fits a line via `linearRegression`,
  predicts `y` at `endDate`.
- Returns `null` (not enough signal) unless there are **≥3 distinct
  snapshots spanning ≥2 minutes of wall-clock time** — a straight line
  through 2 points seconds apart is noise, not a forecast, and showing one
  would overclaim the technique.
- Clamps the projection to `Math.max(projected, currentTotalVotes)` —
  votes only accumulate, so a downward-sloping projection is a modelling
  artifact, not a real prediction, and must never be shown as one.
- Returns a heuristic `confidence` (`'low'` <5 points, `'medium'` 5–14,
  `'high'` 15+) — labelled in the UI as a rough data-volume signal, not a
  statistical confidence interval, to avoid overclaiming rigor the model
  doesn't have.
- Only computes/returns a forecast when `status === 'Active'` — a closed
  election has an actual final count, not a projection.

**`frontend/src/components/TurnoutForecastCard.jsx`**
Presentational card for `ResultsPage`:
- If the hook returns `null`: "Gathering data for a turnout forecast…"
  placeholder (no numbers shown).
- Otherwise: projected final vote count, the election's end date, a
  confidence badge, and a compact `recharts` `LineChart` sparkline of the
  observed snapshots with a dashed segment extending to the projected
  point (recharts is already a project dependency — no new package).
  Caption: "Estimated from N snapshots observed in this browser — not the
  official count."

## Existing files touched
- **`ResultsPage.jsx`** — call `useTurnoutForecast`, render
  `<TurnoutForecastCard />` under the existing chart/leaderboard, only when
  `results.status === 'Active'`.
- **`ElectionsPage.jsx`** — after the existing `GET /elections` fetch
  resolves, call `pruneStaleElections(elections.map(e => e.electionId))`
  for the localStorage garbage-collection described above.

## Tests (Vitest + RTL, matching existing colocated `*.test.jsx` pattern)
- `linearRegression.test.js` — known-slope fixture data, verifies
  `fitLine`/`predict` math directly.
- `turnoutHistory.test.js` — dedupe, 300-point cap, prune behavior, using
  a mocked `localStorage`.
- `TurnoutForecastCard.test.jsx` — renders placeholder with <3 points;
  renders projected number + caption once threshold data is supplied.

## Explicitly out of scope
- No backend changes, no new API endpoints, no schema/migration.
- No new npm dependencies.
- No cross-device sync — the forecast is per-browser, by design (that's
  the tradeoff of the "localStorage history" choice); this is called out
  in the UI caption so it never reads as an authoritative statistic.
