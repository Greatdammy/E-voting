# Build Plan — E-Voting System

Phased plan for Claude Code to scaffold and build the backend and frontend.
Execute phases in order. **Stop after each phase for review before continuing.**
Each phase lists the prompt to give Claude Code and the acceptance check.

---

## Phase 0 — Repository scaffold

**Prompt to Claude Code:**
> Read CLAUDE.md. Create the solution structure only — no business logic yet.
> Backend: a `backend/` folder with an `EVoting.sln` and four projects
> (EVoting.Domain, EVoting.Application, EVoting.Infrastructure, EVoting.API)
> wired with the correct project references per the dependency rule. Frontend:
> a `frontend/` Vite + React 18 app with Redux Toolkit, Axios, React Router,
> Tailwind, and Recharts installed. Add a root `.gitignore` and `README.md`.
> Show me the folder tree before creating anything.

**Acceptance:** `dotnet build` succeeds on an empty solution; `npm run dev`
serves a blank Vite app; dependency references match CLAUDE.md.

---

## Phase 1 — Domain + database

**Prompt:**
> Plan first. Implement the Domain entities (User, Election, Candidate, Vote,
> VoterElectionStatus, OtpToken, AuditLog) and the EF Core AppDbContext in
> Infrastructure, matching the schema and constraints in CLAUDE.md exactly —
> including the composite UNIQUE (VoterId, ElectionId) on Votes. Generate the
> initial migration command for me to run manually; do not apply it.

**Acceptance:** Entities match the schema tables; the unique constraint is
expressed in `OnModelCreating`; migration generated but not applied.

---

## Phase 2 — Auth module (register → OTP → JWT)

**Prompt:**
> Plan first. Build the authentication slice end to end: AuthController with
> register, login, verify-otp; an IAuthService in Application; BCrypt password
> hashing (factor 12), CSPRNG 6-digit OTP hashed with 10-min expiry, JWT issued
> on OTP verification (HMAC-SHA256, 8h, UserId + role claims). Wire SendGrid
> behind an IEmailService for OTP delivery. Add FluentValidation validators and
> the rate-limit policy (10/min/IP) on auth endpoints. Keep the JWT key and
> SendGrid key in env vars.

**Acceptance:** Registration rejects duplicate emails; OTP is single-use and
expires; a valid JWT is returned only after correct OTP; no secrets in source.

---

## Phase 3 — Elections, candidates, voting + SignalR

**Prompt:**
> Plan first. Implement election lifecycle (Upcoming → Active → Closed),
> admin candidate CRUD, and the vote submission flow. The vote handler must:
> open a DB transaction, check voting status, store SHA-256(UserId) as VoterId,
> write the vote, update VoterElectionStatus, commit, then broadcast the new
> tally over a SignalR ResultsHub. Return the confirmation hash. Enforce RBAC
> with [Authorize(Roles=...)]. Build to the API contract in CLAUDE.md.

**Acceptance:** A second vote by the same voter in the same election is rejected
by both the app check and the DB constraint; tally broadcasts on each vote;
ballot anonymisation verified (no raw UserId in Votes).

---

## Phase 4 — Frontend

**Prompt:**
> Plan first. Build the React SPA: an Axios instance with a JWT request
> interceptor; a Redux auth slice (token, role, expiry); a ProtectedRoute
> wrapper; and pages for Register, Login + OTP, Election list, Ballot, and a
> live Results dashboard. The Results page opens a SignalR connection and
> updates a Recharts bar chart on each vote message. Add an Admin area to create
> elections and manage candidates, guarded by role. Style with Tailwind.

**Acceptance:** Full voter flow works against the running API; expired token
redirects to login before any API call; results chart updates live.

---

## Phase 5 — Tests + hardening

**Prompt:**
> Plan first. Add xUnit + Moq unit tests for AuthService and VoteService
> (mock repositories) and an integration test covering register → verify-otp →
> vote → results. Add Jest + React Testing Library tests for the OTP form and
> the voting flow. Apply HSTS + HTTPS redirection middleware and confirm CORS,
> auth, authorisation, and rate-limit ordering in the pipeline.

**Acceptance:** Tests pass; double-vote and expired-OTP cases are covered;
middleware order matches the security design.

---

## How to use this with Claude Code

1. `cd evoting-system && claude` (Claude Code auto-loads CLAUDE.md).
2. Paste the Phase 0 prompt. Review the plan it writes to `plans/`, approve.
3. Proceed phase by phase. Never skip the planning step — CLAUDE.md enforces it.
4. Run each migration yourself with the command Claude Code gives you.
