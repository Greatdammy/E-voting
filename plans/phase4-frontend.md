# Phase 4 — React SPA

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Build the frontend against the API contract as it actually exists after
Phases 1–3 (no OTP — login issues the JWT directly): Redux auth slice,
Axios JWT interceptor, `ProtectedRoute`, Register/Login/Elections/Ballot/
Results pages, an Admin area (elections + candidates + user provisioning),
and a live Recharts results chart driven by `ResultsHub`. All packages
needed (`@reduxjs/toolkit`, `react-redux`, `axios`, `react-router-dom`,
`recharts`, `@microsoft/signalr`, `tailwindcss`) are already installed and
pinned from Phase 0 — **no new frontend packages**. Testing (Jest + RTL) is
explicitly Phase 5 scope per PLAN.md; this phase only builds working UI.

## Two backend gaps this phase has to fix (not purely "frontend")
Neither of these is optional — without them the phase's own acceptance
criteria can't be met, so I'm just doing them rather than asking:

1. **No CORS policy exists yet.** The Vite dev server runs on
   `http://localhost:5173`; the API on `https://localhost:7003`. Every
   cross-origin request from the SPA will be blocked by the browser without
   a CORS policy — "full voter flow works against the running API" is
   unachievable otherwise. Adding `builder.Services.AddCors(...)` (policy
   named `"Frontend"`, origin `http://localhost:5173`, any header/method,
   no credentials — the JWT goes in the `Authorization` header, not a
   cookie, so `AllowCredentials()` isn't needed) and `app.UseCors("Frontend")`
   to `Program.cs`, placed after `UseHttpsRedirection()` and before
   `UseRateLimiter()`/`UseAuthentication()`/`UseAuthorization()`. Full
   pipeline-ordering audit is still Phase 5 scope; this is just adding the
   missing piece in a defensible position.
2. **No way for an admin to list elections.** The API contract has
   `POST api/admin/elections` (create) and candidate CRUD, but nothing
   returns the election list an admin needs to navigate into "manage
   candidates for election X." (`GET api/elections` exists but is
   `[Authorize(Roles = "Voter")]` and returns voter-specific `HasVoted`
   data — wrong shape and wrong role for the admin UI.) Adding
   `GET api/admin/elections` → `IElectionService.ListElectionsAsync()` →
   reuses the existing `ElectionResponseDto` and `IElectionRepository.ListAsync()`
   already built in Phase 3, RBAC matching `AdminController`'s class-level
   `Administrator,ElectionOfficer` policy.

## Redux auth slice
`store/authSlice.js` — state: `{ token, userId, role, expiresAt }`, all
`null` initially. `initialState` lazily reads from `localStorage` (key
`evoting_auth`) so a page refresh doesn't silently log the user out —
CLAUDE.md doesn't mandate persistence, but losing auth state on every
refresh would make the app unusable for a real UAT flow, so I'm treating
this as an obvious inclusion, not a question. `setCredentials(payload)`
writes to both the store and `localStorage`; `logout()` clears both. No
`redux-persist` dependency — this is small enough to do by hand in the two
slice reducers, not worth adding a new package for.

## Axios instance (`api/axiosInstance.js`)
`baseURL` built from a single `VITE_API_ORIGIN` env var (default
`https://localhost:7003`, committed in `frontend/.env.development` — not a
secret, just a local dev default, overridable via an untracked
`.env.local` if your port differs) as `${VITE_API_ORIGIN}/api`. Request
interceptor reads `store.getState().auth.token` and sets the `Authorization:
Bearer` header when present. Response interceptor: on `401`, dispatches
`logout()` and redirects to `/login` — a reactive backstop alongside
`ProtectedRoute`'s proactive client-side expiry check (belt-and-braces:
`ProtectedRoute` stops the request from ever being sent once the token's
visibly expired; the interceptor catches the case where the server rejects
a token the client still thought was valid — a small, direct addition, not
speculative future-proofing).

## `ProtectedRoute`
Matches CLAUDE.md's spec literally: checks the store "on every route
change" (a render-time check, not a running timer) — token present and
`expiresAt` in the future; role in `allowedRoles` if that prop is given.
Fails either check → `logout()` + `<Navigate to="/login" replace />` before
any nested route renders (so no child page's data-fetch effect ever fires
with a stale/absent token). Role mismatch (e.g. a Voter hitting `/admin`)
redirects to `/elections` instead of `/login`, since they are authenticated,
just not authorized for that section.

## Pages
- **Register** (`/register`) — FullName, Email, Password, client-side-only
  ConfirmPassword check → `POST api/auth/register` → success message,
  redirect to `/login` with the email prefilled. No auto-login (Phase 2's
  register doesn't issue a token — only login does).
- **Login** (`/login`) — Email, Password → `POST api/auth/login` →
  `setCredentials`, redirect by role: `Voter` → `/elections`,
  `Administrator`/`ElectionOfficer` → `/admin`.
- **Elections** (`/elections`, Voter-only) — `GET api/elections` → list with
  status + `HasVoted`; Active + not-voted rows link to the ballot, others
  link to results.
- **Ballot** (`/elections/:id/ballot`, Voter-only) — `GET .../ballot` →
  radio-select candidate → `POST .../vote`. On success, the form is replaced
  in place by a confirmation panel (`VoteId`, `ConfirmationHash`) — the
  voter's receipt — with a link to the results page, rather than
  auto-redirecting past it.
- **Results** (`/elections/:id/results`, public — no `ProtectedRoute`) —
  `GET .../results` for the initial render, then opens a SignalR connection
  to `${VITE_API_ORIGIN}/hubs/results`, calls `JoinElection(electionId)`,
  and updates a Recharts bar chart on every `ReceiveResults` push.
  Connection is started on mount and stopped on unmount.
- **Admin** (`/admin/*`, `Administrator` + `ElectionOfficer`):
  - `/admin/elections` — list (the new `GET api/admin/elections`) + create
    form.
  - `/admin/elections/:id/candidates` — list/create/edit/delete, calling the
    Phase 3 candidate CRUD endpoints. Delete uses a plain `window.confirm()`
    — not pulling in a modal library for one confirmation dialog.
  - `/admin/users` — Create User (FullName, Email, Password, Role select) →
    `POST api/admin/users`. This link/page only renders for
    `role === 'Administrator'` in the nav and is additionally
    `ProtectedRoute`-gated to `Administrator` only, matching the backend's
    action-level override (`ElectionOfficer` can reach `/admin` but not
    user provisioning, on either side of the wire).

## Components
- `NavBar` — role-aware links (logged out: Login/Register; Voter: Elections;
  Admin/Officer: Admin, +Create User if Administrator; always: Logout when
  authenticated).
- `ResultsChart` — thin Recharts `BarChart` wrapper taking a tally array,
  reused by the Results page (and could be reused by an admin
  "preview results" view later, though that's not in this phase's scope).

## Error display
No toast/notification library — inline error text under each form
(`response.data.message` for the `{ message }` shape our controllers
already return on business failures, falling back to flattening
`response.data.errors` for ASP.NET Core's built-in `ValidationProblem()`
shape on FluentValidation failures, then a generic fallback string). Keeps
this phase's dependency footprint at zero new packages.

## New files
```
frontend/src/
  config.js                          — API_ORIGIN / API_BASE_URL / SIGNALR_HUB_URL
  api/axiosInstance.js
  store/store.js
  store/authSlice.js
  routes/ProtectedRoute.jsx
  signalr/resultsConnection.js
  components/NavBar.jsx
  components/ResultsChart.jsx
  pages/RegisterPage.jsx
  pages/LoginPage.jsx
  pages/ElectionsPage.jsx
  pages/BallotPage.jsx
  pages/ResultsPage.jsx
  pages/admin/AdminElectionsPage.jsx
  pages/admin/AdminCandidatesPage.jsx
  pages/admin/AdminCreateUserPage.jsx
frontend/.env.development            — VITE_API_ORIGIN=https://localhost:7003
```
Rewritten: `App.jsx` (routes + `NavBar`), `main.jsx` (wraps with Redux
`Provider` + `BrowserRouter`).

Backend additions (the two gaps above):
- `EVoting.Application/Interfaces/IElectionService.cs` — add
  `ListElectionsAsync()`
- `EVoting.Application/Services/ElectionService.cs` — implement it
- `EVoting.API/Controllers/AdminController.cs` — add `GET elections`
- `EVoting.API/Program.cs` — add `AddCors`/`UseCors`

## Acceptance check (per PLAN.md)
- Full voter flow (register → login → view elections → vote → see receipt →
  view live results) works against the running API.
- Expired or absent token redirects to `/login` before any API call fires.
- Results chart updates live via SignalR without a page refresh.

## Open question for you before I write any code
1. OK with the two backend additions (CORS policy, `GET api/admin/elections`)
   landing in this "frontend phase" rather than being deferred/retrofitted
   into Phase 3? Both are small and this phase's stated acceptance criteria
   can't be met without them, so my default is to just include them — say
   if you'd rather I stop and get separate sign-off on backend changes
   first.
