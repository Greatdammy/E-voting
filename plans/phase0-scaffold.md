# Phase 0 — Repository Scaffold

Status: PROPOSED — awaiting approval. No source files created yet.

## Goal
Stand up the empty solution/project structure for both backend and frontend,
wired per the Clean Architecture dependency rule, with no business logic.
Acceptance is a successful `dotnet build` and a serving blank Vite app.

## Scope
- `backend/`: .NET 8 solution with four projects, correct project references.
- `frontend/`: Vite + React 18 app with the pinned frontend packages installed.
- Root `.gitignore` and `README.md`.
- No entities, no controllers, no components beyond the Vite default template.
- No `git init` / no commits — this step only creates files on disk. (Confirm
  in review if you want the repo initialized as a git repo at this point.)

## Proposed folder tree

```
evoting/
├── CLAUDE.md
├── PLAN.md
├── README.md
├── .gitignore
├── plans/
│   └── phase0-scaffold.md
├── backend/
│   ├── EVoting.sln
│   ├── src/
│   │   ├── EVoting.Domain/
│   │   │   └── EVoting.Domain.csproj
│   │   ├── EVoting.Application/
│   │   │   └── EVoting.Application.csproj
│   │   ├── EVoting.Infrastructure/
│   │   │   └── EVoting.Infrastructure.csproj
│   │   └── EVoting.API/
│   │       ├── EVoting.API.csproj
│   │       ├── Program.cs
│   │       └── appsettings.json        (placeholders only, per CLAUDE.md)
│   └── tests/                          (empty placeholder dirs for Phase 5)
│       ├── EVoting.UnitTests/
│       └── EVoting.IntegrationTests/
└── frontend/
    ├── index.html
    ├── package.json
    ├── vite.config.js
    ├── tailwind.config.js
    ├── postcss.config.js
    └── src/
        ├── main.jsx
        ├── App.jsx
        └── (default Vite/React scaffold files)
```

## Backend details

**Solution:** `backend/EVoting.sln`, referencing all four projects under `backend/src/`.

**Project references (dependency rule, outer → inner only):**
- `EVoting.Domain` — no project references. Target framework: `net8.0`.
- `EVoting.Application` — references `EVoting.Domain`.
- `EVoting.Infrastructure` — references `EVoting.Application` and `EVoting.Domain`.
- `EVoting.API` — references `EVoting.Application` only (never Infrastructure or
  Domain directly, per CLAUDE.md — DI wiring in `Program.cs` registers
  Infrastructure implementations against Application interfaces via the
  Infrastructure project's assembly reference, added as a package/project
  reference solely for composition-root registration, not for type usage in
  API code).

  Note: In practice ASP.NET Core composition roots typically need a reference
  to Infrastructure to call its `AddInfrastructure(...)` DI extension method.
  I will add `EVoting.API → EVoting.Infrastructure` as a reference for that
  sole purpose (DI registration in `Program.cs`), and treat it as a rule: API
  code must never import Infrastructure types directly outside `Program.cs`.
  Flagging this now since CLAUDE.md's dependency rule diagram doesn't show
  it — let me know if you'd rather solve composition differently (e.g., a
  separate `EVoting.Composition` project).

**Empty projects only:** each `.csproj` created via `dotnet new classlib` (Domain,
Application, Infrastructure) and `dotnet new webapi` (API, controllers-based,
no minimal-API sample endpoints kept). No NuGet packages added yet beyond what
`dotnet new webapi` scaffolds by default — EF Core, JWT, SendGrid, BCrypt,
FluentValidation packages arrive in the phases that use them (1, 2) so each
phase's dependencies are visible in its own diff.

**`appsettings.json`:** placeholder keys only —
`"ConnectionStrings:Default": ""`, `"Jwt:Key": ""`, `"SendGrid:ApiKey": ""` —
with a comment/README note that real values come from user-secrets or env
vars, never committed.

**Test projects:** empty `EVoting.UnitTests` (xUnit) and
`EVoting.IntegrationTests` (xUnit) project shells added to the solution now
(structure only), so Phase 5 has a home without touching the `.sln` again.
No test packages beyond the xUnit template default.

## Frontend details

- Scaffolded via `npm create vite@latest frontend -- --template react`.
- Packages installed: `redux`, `@reduxjs/toolkit`, `react-redux`, `axios`,
  `react-router-dom`, `recharts`, `tailwindcss` (+ `postcss`, `autoprefixer`),
  `@microsoft/signalr` (needed starting Phase 4, but installing now keeps
  package.json stable across phases — flagging this choice, can defer to
  Phase 4 instead if you prefer strict phase-by-phase dependency additions).
- Tailwind initialized (`tailwind.config.js`, `postcss.config.js`), base
  directives added to the default CSS entry point.
- No routes, no Redux slices, no components beyond the Vite template's default
  `App.jsx` — that logic starts in Phase 4.

## Root files
- `.gitignore`: standard .NET (`bin/`, `obj/`) + Node (`node_modules/`,
  `dist/`) + `.env`, `*.user`, `appsettings.Development.json` (in case real
  local secrets ever land there).
- `README.md`: project name/description, prerequisites (.NET 8 SDK, Node 18+,
  SQL Server 2022), and how to run backend (`dotnet run` from `EVoting.API`)
  and frontend (`npm run dev`) once later phases add logic.

## Explicitly out of scope for Phase 0
- No entities, DbContext, migrations (Phase 1).
- No auth, controllers logic beyond default template (Phase 2+).
- No SignalR hub implementation (Phase 3).
- No Redux slices, routing, pages (Phase 4).
- No tests beyond empty project shells (Phase 5).

## Acceptance check (per PLAN.md)
- `dotnet build` succeeds on the empty solution.
- `npm run dev` serves the blank Vite app.
- Project references match the dependency rule (with the API→Infrastructure
  composition-root caveat flagged above for your sign-off).

## Open questions for you before I create anything
1. Should I `git init` the repo now, or leave that to you?
2. OK with `EVoting.API` referencing `EVoting.Infrastructure` solely for
   `Program.cs` DI wiring (standard ASP.NET Core pattern), or do you want a
   different composition mechanism?
3. OK installing `@microsoft/signalr` in Phase 0 for a stable `package.json`,
   or defer that install to Phase 4 when it's first used?
