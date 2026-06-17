---
agent: agent
description: Add or update an Entity Framework Core DbContext, table column, or hand-written migration.
---

# Add an EF Core Migration

Make a database schema change via EF Core. Invoke the **add-ef-migration** skill and follow `.ai/rules/efcore.md` (+ `.ai/rules/efcore.specs.md`).

> Applies only to projects that use EF Core.

## Confirm first

- **Change type** (new table / column / relationship / rename), the **entity**, and its **feature DbContext**.

## Non-negotiables

- Migrations are **hand-written** in the `Database` project — never `dotnet ef migrations add` / `database update`.
- Version-named files `v{major}_{minor}_{patch}.cs` in a folder matching the entity category; namespace matches the folder.
- Use the cross-database column helpers (`StringColumn`, `GuidColumn`, `NumberColumn<T>`, `DateTimeOffsetColumn`) and `WellKnownTables` constants — never raw `table.Column<T>()` or magic strings.
- Never hardcode a provider (`UseSqlite`/`UseNpgsql`) — use `UseDatabaseFromConnectionString`.
- Never mutate state directly through a DbContext — writes flow through Chronicle events.

The skill carries the step-by-step detail; don't duplicate it here.
