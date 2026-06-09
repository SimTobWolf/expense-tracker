# Project: ExpenseTracker

> Project-specific rules for agents. The lab Constitution at `agents-lab/CLAUDE.md` still applies — this file ADDS or OVERRIDES rules for this project only. Where the two conflict, this file wins.

## What this is

A personal expense tracker SaaS built end-to-end by the agents lab as its first proving-ground. Users record expenses with categories and dates, then see aggregations.

This project's primary purpose is to **exercise the lab pipeline**, not to ship a real product. Decisions favor proving the pipeline works.

## Stack

- **Runtime:** .NET 9 (TargetFramework `net9.0`)
- **API:** ASP.NET Core Minimal API
- **UI:** Blazor — NOT bootstrapped. Add via a pipeline feature when needed.
- **Tests:** xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) for integration tests
- **Persistence:** TBD by the architect on first data-storing feature. Default candidate: SQLite via EF Core, then revisit if Postgres becomes worth the weight.

## Layout

```
projects/expense-tracker/
├── ExpenseTracker.sln
├── CLAUDE.md                       ← you are here
├── src/
│   └── ExpenseTracker.Api/         ← minimal API project
└── tests/
    └── ExpenseTracker.Api.Tests/   ← xUnit integration tests
```

New code goes under `src/` per .NET conventions. New test projects go under `tests/` and mirror the source project name with `.Tests` suffix. Architects must follow this layout.

## Conventions

- **C# 12 features ok.** File-scoped namespaces, top-level statements, primary constructors.
- **Nullable reference types:** enabled by default in templates. Keep enabled.
- **Naming:** PascalCase for types/methods, camelCase for locals/parameters, `_camelCase` for private fields, `I` prefix for interfaces.
- **Records for DTOs**, classes for services with behavior.
- **No `var` for primitives** (`string`, `int`, `bool`). `var` is fine for clearly-typed expressions.
- **No `using` block forests.** Group: System.*, third-party, project.

## Testing rules

- **Integration tests over mocks** for endpoints. Use `WebApplicationFactory<Program>` — the API project already exposes `public partial class Program {}` for this purpose.
- **Test names are Given/When/Then sentences.** See `HealthEndpointTests` for the pattern.
- **One test per behavior.** No bundled assertions.
- **Database tests use a real database** — in-memory provider is forbidden for persistence-layer tests because it hides query translation bugs. SQLite in a temp file is fine; replace with a containerized Postgres when we add it.

## Run commands

```powershell
# from projects/expense-tracker/
dotnet run --project src/ExpenseTracker.Api        # http://localhost:5xxx/health
dotnet test                                         # all tests
dotnet test --filter <classname>                    # one test class
```

## What's deliberately NOT here yet

These are gaps the pipeline is expected to fill. None of them is a bug.

- No authentication. First feature that needs it must add it.
- No persistence. First feature that needs state must add it (and create the migration story).
- No Blazor UI. The API is verifiable via curl until a UI feature is requested.
- No Docker / deployment story.
- No logging / observability setup beyond the .NET default.

## Project-specific guardrails (additions to lab Constitution)

- **Never use `dotnet run` for tests.** Always `dotnet test`. The two have different startup paths.
- **Migrations are reviewed independently.** A schema change is a separate PR from the feature that uses it.
- **No `[Fact]` without assertions.** Lint will flag empty test bodies; do not let them slip past.
- **`Program.cs` stays minimal.** Composition root only. Wire DI, map endpoints, run. Logic moves to feature folders.
