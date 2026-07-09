# Frontend Redesign — Modern SaaS Visual Refresh

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Visual-only redesign of the existing React SPA — every page, same routes,
same Redux logic, same Axios calls, same validation, same error handling.
Nothing about *what* the app does changes, only how it looks. Per your
answers: Modern SaaS style (soft shadows, rounded cards, indigo/violet
accent, generous whitespace), dark mode (system-preference default + manual
toggle, persisted), `lucide-react` icons.

## Hard constraint: don't break the Phase 5 test suite
`LoginPage.test.jsx` and `BallotPage.test.jsx` query by `placeholder`
(`"Email"`, `"Password"`), button accessible name (`/login/i`, `/submit
vote/i`), and `getByRole('radio')` for candidate selection. The redesign
keeps all of that intact — e.g. candidate selection becomes a styled
clickable card, but it's still a real `<input type="radio">` underneath
(visually customized via a wrapper, not replaced with a div+onClick), so
`getByRole('radio')` keeps working. I'll rerun `npm test` after the
redesign to confirm nothing regressed rather than assuming.

## Design system
**Colors** — indigo-600 (light) / indigo-500 (dark) as the primary accent;
violet paired with it for subtle gradient touches (auth page background,
card highlights). Neutrals move from the current default `gray` to `slate`
(cooler, more "modern SaaS" than Tailwind's default gray). Semantic colors
for status: emerald (Active), amber (Upcoming), rose (Closed/errors), sky
(info).

**Typography** — Inter via Google Fonts (linked in `index.html`), falling
back to the existing system-font stack if it fails to load. Standard,
widely-used modern-SaaS font choice.

**Dark mode** — `tailwind.config.js` switches from Tailwind's default
`media` strategy to `darkMode: 'class'`, so a toggle can override the
system preference. A small `useDarkMode` hook: reads `localStorage`, falls
back to `prefers-color-scheme`, toggles the `dark` class on `<html>`,
persists the user's explicit choice once they use the toggle.

**Reusable UI primitives** (new — this is the right amount of abstraction
now, given 8 pages currently hand-roll the same button/input/card
`className` strings repeatedly):
- `components/ui/Button.jsx` — primary/secondary/danger/ghost variants
- `components/ui/Card.jsx`
- `components/ui/Input.jsx` — labeled input, optional leading icon
- `components/ui/Badge.jsx` — status pill (maps `Active`/`Upcoming`/`Closed`
  to emerald/amber/rose)
- `components/ui/Spinner.jsx` — loading indicator (replaces plain "Loading…"
  text)

## Page-by-page treatment
- **NavBar** — sticky, brand mark, role-aware links with icons, dark-mode
  toggle (sun/moon), responsive (collapses to a menu on small screens).
- **Login / Register** — centered card over a subtle gradient background,
  icon-prefixed inputs (mail/lock icons), consistent with each other.
- **Elections** — card grid instead of a plain list; status badges; clearer
  primary action per card (Vote / Results) with icons.
- **Ballot** — candidates as selectable cards (real radio input underneath,
  styled wrapper — see constraint above); redesigned confirmation panel
  with a success icon and a cleaner receipt layout.
- **Results** — I'll invoke the `dataviz` skill before touching
  `ResultsChart.jsx` specifically, since it's a chart and that skill's
  guidance (color choice, mark specs, accessibility) applies directly. Page
  gets a "live" status indicator and a leaderboard-style tally alongside
  the chart (leading candidate highlighted).
- **Admin pages** (Elections, Candidates, Create User) — consistent card/
  form styling matching the voter-facing pages; candidate list becomes a
  cleaner data list with icon buttons for edit/delete.

## New dependency
`lucide-react` (frontend only) — tree-shakeable icon set, only the icons
actually imported end up in the bundle.

## New/changed files
New: `components/ui/{Button,Card,Input,Badge,Spinner}.jsx`, `hooks/useDarkMode.js`
Changed (styling/structure only, no logic changes): `tailwind.config.js`,
`index.html`, `src/index.css`, `components/NavBar.jsx`, all `pages/*.jsx`
and `pages/admin/*.jsx`, `components/ResultsChart.jsx`, `App.jsx`

## Verification
`npm run build` succeeds, `npm test` still passes (6/6, unmodified), and a
manual click-through of the running app (you've got a live backend now) to
confirm nothing functionally broke — matching this session's own standard
of verifying against the real app, not just tests.
