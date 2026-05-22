---
agent: agent
description: Add or update an Entity Framework Core DbContext, table column, or hand-written migration in the project.
---

# Add an EF Core Migration

I need to make a **database schema change** via Entity Framework Core.

> **Before starting:** Read `.github/instructions/efcore.instructions.md` and `.github/instructions/efcore.specs.instructions.md`.

## Inputs

- **Change type** — one of:
  - New table / entity
  - New column on existing table
  - Remove column
  - Rename column / table
  - New relationship (FK, navigation property)
  - Other (describe)
- **Entity name** — the C# record being changed
- **Feature DbContext** — which `ReadOnlyDbContext` or `BaseDbContext` owns this entity

## Step-by-step process

### 1 — Update the entity and feature DbContext

- Add/remove/rename properties on the entity `record` in the **Core** project, co-located with its feature folder.
- Update or create the feature DbContext:
  - Inherit from `ReadOnlyDbContext` (read models) or `BaseDbContext` (writable state) — never raw `DbContext`
  - Expose `DbSet<T>` as expression-bodied properties: `public DbSet<Entity> Entities => Set<Entity>();`
  - One focused context per feature — never a "god context"

### 2 — Add the table name to WellKnownTables

If this is a new table, add a `const string` to `Database/WellKnownTables.cs` before writing the migration.

### 3 — Write the migration by hand

Migrations are **hand-written** in the **Database** project — never use `dotnet ef migrations add`.

- Place the file in a subfolder matching the entity category: `Database/Missions/v1_1_0.cs`
- Use `v{major}_{minor}_{patch}.cs` naming — never PascalCase names like `AddColumn`
- Namespace must match the folder: `namespace Database.Missions;`
- Always use Cratis Arc cross-database column helpers (`table.StringColumn()`, `table.GuidColumn()`, `table.NumberColumn<T>()`, `table.DateTimeOffsetColumn()`) — never raw `table.Column<T>()`
- Always reference table names from `WellKnownTables` constants — never magic strings

### 4 — Register the DbContext (if new)

- Read-only contexts are auto-discovered via `AddReadModelDbContextsWithConnectionStringFromAssemblies`
- Writable contexts need explicit `AddDbContextWithConnectionString<T>` in Infrastructure

### 5 — Update specs

- Integration specs using in-memory SQLite pick up schema changes via `context.Database.EnsureCreated()`.
- Follow `.github/instructions/efcore.specs.instructions.md` for testing patterns.

### 6 — Validate

Run `dotnet build` and `dotnet test`. Fix all failures before completing.

## Key rules

- **Never** use `dotnet ef migrations add` or `dotnet ef database update`
- **Never** hardcode a provider (`UseSqlite`, `UseNpgsql`) — use `UseDatabaseFromConnectionString`
- **Never** mutate state directly through a DbContext — writes flow through Chronicle events
- **Always** use `WellKnownTables` constants and cross-database column helpers
