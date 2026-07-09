# Phase 6 — Startup Auto-Migration

Status: PROPOSED — awaiting approval. No source files touched yet.

## Goal
Add `context.Database.Migrate()` to `Program.cs`, gated behind a
`Database:AutoMigrate` config flag (env var `Database__AutoMigrate` on
deploy). Needed because site4now.net shared hosting gives no shell access
to run `dotnet ef database update` manually after publishing.

## Why gated, not unconditional
CLAUDE.md's migration rule ("never auto-apply migrations... without
showing the migration first") is about *generating* migrations blind —
that's unaffected: migrations are still created locally via
`dotnet ef migrations add <Name>`, reviewed, and committed as files before
this code ever runs. This change only applies migrations that are already
committed and already reviewed. Gating behind a flag that defaults to
**off** means:
- Local dev / `dotnet run` / integration tests: unaffected, flag absent.
- Production: only applies when you deliberately set
  `Database__AutoMigrate=true` in `web.config`, so it's an explicit,
  visible decision at deploy time, not silent-by-default behavior.

## Change — `Program.cs`
Insert an `if` before/around the existing admin-seed scope block (lines
124–130), reusing the same `AppDbContext` and `ILogger<Program>` already
resolved there rather than opening a second scope:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        logger.LogInformation("Database:AutoMigrate is true — applying pending EF Core migrations.");
        await context.Database.MigrateAsync();
    }

    await AdminSeeder.SeedAdminAsync(context, passwordHasher, builder.Configuration, logger);
}
```

`MigrateAsync()` is a no-op if the database is already up to date, so this
is safe to leave the flag on across repeated deploys/restarts.

## Config surface
- `appsettings.json` — add `"Database": { "AutoMigrate": false }` as the
  committed default (explicit `false`, not just absent, so the setting is
  discoverable).
- `web.config.template` — add commented `Database__AutoMigrate` entry
  (default guidance: `false` for normal deploys, `true` only for the
  deploy that ships a new migration, then optionally back to `false`).

## Out of scope
No changes to `Spa:Serve`, `Frontend:AllowedOrigin`, or SMTP/email config
— those were already flagged as not-yet-implemented and aren't part of
this ask.
