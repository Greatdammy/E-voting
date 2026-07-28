# Frontend AI Modernization — Visual Layer

Status: IMPLEMENTED (2026-07-28), with two adjustments from the original text:
1. The Voter Assistant was dropped entirely (paid API — see
   `ai-voter-assistant.md`), so there is no `AssistantWidget` mount point in
   `App.jsx` and no AI-thinking Spinner usage for it.
2. The NavBar never got a top-level "Integrity" link — alerts are
   per-election, so the entry point is an "Integrity" button on each
   election's card in `AdminElectionsPage`, matching the existing "Manage
   candidates" pattern (this was already true before this pass; noted here
   for completeness).

Everything else shipped as written: the shared fuchsia "AI accent" (Sparkles
icon + `text-fuchsia-600 dark:text-fuchsia-400`), applied to
`TurnoutForecastCard`, the Elections-page insight chip, the Results-page
integrity badge, the Integrity Guard header icon, and a small permanent
Sparkles badge on the NavBar logo mark; plus the `Spinner` "ai" variant used
for `TurnoutForecastCard`'s gathering-data state. Contrast was checked by
hand (WCAG formula) rather than via the dataviz skill's validator script,
because `node` is not on PATH in this environment (npm shims exist under
`C:\Program Files\nodejs` but `node.exe` itself is missing) — `npm test` /
`npm run build` could not be run either; verify both yourself before
treating this as fully confirmed.

## Context
The Modern SaaS visual redesign (`plans/frontend-redesign.md`) and the first
AI feature (turnout forecast, `plans/turnout-forecast.md`) are already shipped
— dark mode, indigo/violet accent, `lucide-react` icons, Tailwind `slate`
neutrals, and reusable `ui/` primitives (`Button`, `Card`, `Input`, `Badge`,
`Spinner`) all exist in `frontend/src/`. This plan is the **next layer**: make
the app visually read as an AI-assisted product end to end, not just one
card, and give the two new features (`plans/ai-integrity-guard.md`,
`plans/ai-voter-assistant.md`) a consistent visual language so a user can
tell "AI-derived" content apart from authoritative data at a glance — which
also happens to reinforce the project's election-integrity requirement that
estimates/flags are never confused with the official tally.

## Design decision: a dedicated "AI accent," distinct from the brand accent
Add one new color token used **exclusively** for AI-surfaced UI — a subtle
violet-to-fuchsia gradient/glow, paired with a `Sparkles` icon
(lucide-react, already a dependency, no new package). The existing
indigo/violet gradient stays the brand mark (logo, primary buttons, links);
the new AI accent appears only on: the turnout forecast card, integrity
alerts, the assistant widget, and any AI-derived copy. This restraint is
deliberate — the color's whole job is to answer "is this AI-generated or
the real count," so it can't also be the everyday brand color. I'll invoke
the `dataviz` skill before touching any chart/color specifics, since that
skill's palette-validation tooling applies directly here (same process
already used for `ResultsChart.jsx` and `TurnoutForecastCard.jsx`).

## Page-by-page treatment

- **Global (`App.jsx`)** — mount `AssistantWidget` once in the layout shell
  (from `plans/ai-voter-assistant.md`), so it persists across voter routes.

- **NavBar** — add the Integrity link for Admin/Officer roles (Shield/
  ShieldAlert icon, from `plans/ai-integrity-guard.md`). Add a small
  `Sparkles` accent to the existing gradient logo mark — a subtle, permanent
  visual cue that the platform has AI-assisted features, not a full
  rebrand.

- **Elections page** — each *Active* election card gets a one-line "AI
  insight" chip (e.g. "Closing soon — turnout trending up"), reusing the
  already-computed `useTurnoutForecast` data with zero new backend calls.
  Styled with the new AI accent + `Sparkles` icon so it reads as
  algorithmic, not editorial.

- **Results page** — for Admin/Officer viewers, an understated "Monitored by
  an automated integrity check" badge linking to the new
  `AdminIntegrityPage`; no alert detail leaks to public viewers (see
  `plans/ai-integrity-guard.md`'s scope boundary). The existing
  `TurnoutForecastCard` gets restyled to use the new shared AI accent
  token instead of its current one-off indigo styling, so it's visually
  consistent with the integrity alerts and the assistant widget.

- **Admin Integrity page (new)** — full page described in
  `plans/ai-integrity-guard.md`; styled with the Card/Badge primitives
  already in `components/ui/`, extended only with the new AI-accent
  severity variants (Info=sky, Warning=amber, Critical=rose — reusing the
  existing semantic status colors from the original redesign rather than
  inventing new ones for severity, to keep the palette small).

- **Loading/AI-thinking state** — a small extension to the existing
  `Spinner` primitive: an alternate "AI thinking" treatment (subtle pulse
  in the AI accent color rather than the default neutral spin) used
  specifically while `useTurnoutForecast` is gathering data, while
  integrity detection is computing, and while the assistant widget awaits
  a reply — so users learn to distinguish "waiting on AI" from "waiting on
  network," without adding a second spinner component from scratch.

## New/changed files
New: `components/AssistantWidget.jsx`, `hooks/useAssistant.js`,
`pages/admin/AdminIntegrityPage.jsx`, `signalr/integrityConnection.js` (or an
extension to the existing `resultsConnection.js`), a small `aiAccent`
color/token addition inside `tailwind.config.js` or `index.css` (whichever
the existing dark-mode/color setup already uses — I'll match that pattern
rather than introduce a second theming mechanism).

Changed (styling only, no logic changes to already-shipped features):
`App.jsx` (mount point), `NavBar.jsx` (new link + logo accent),
`ElectionsPage.jsx` (insight chip), `ResultsPage.jsx` (integrity badge +
`TurnoutForecastCard` restyle), `components/ui/Spinner.jsx` (AI-thinking
variant), `components/ui/Badge.jsx` (severity variants if not already
generic enough).

## No new dependencies beyond the two feature plans
Everything here reuses `lucide-react`, Tailwind, and the existing `ui/`
primitives already in the project. The only new dependencies in this whole
AI initiative are the two named in the feature plans:
`Microsoft.ML`/`Microsoft.ML.TimeSeries` (backend, integrity guard — flagged
for your decision there) and the Anthropic .NET SDK (backend, voter
assistant).

## Verification
Same standard as the original redesign: `npm run build` succeeds, `npm test`
still passes with no regressions to the existing suite (`LoginPage.test.jsx`,
`BallotPage.test.jsx`, `TurnoutForecastCard.test.jsx`, etc. — none of their
query selectors change), plus a manual click-through once the backend
features are implemented, per this session's standing rule of verifying
against the real running app rather than tests alone.

## Suggested build order
1. `plans/ai-integrity-guard.md` backend (Domain → Application →
   Infrastructure → API → migration) — no frontend dependency, can be built
   and tested standalone first.
2. `plans/ai-voter-assistant.md` backend — same, standalone.
3. This plan's frontend work, once both backends exist to build the real
   pages against (the AI-accent token and Spinner variant can be built
   earlier and reused by both).
