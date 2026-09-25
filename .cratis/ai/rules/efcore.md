---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
profile: application
---
<!-- cratis-ai-managed: rules/efcore.md -->

# Entity Framework Core Instructions

> **⚠️ APPLIES ONLY TO PROJECTS USING ENTITY FRAMEWORK CORE**
> If your project does not reference `Microsoft.EntityFrameworkCore` or any EF Core packages, **ignore this entire file**. These rules are irrelevant outside of EF Core contexts.

`Cratis.Arc.EntityFrameworkCore` gives an Arc application EF Core-backed read models: two `DbContext` base types, convention-based registration, provider selection from the connection string, cross-database column helpers for migrations, concept-aware value conversion, and live observation of a `DbSet`. **That is the whole of what Arc ships** — it has no migration runner, no table-name constant convention, no design-time factory and no prescribed project split. Where an application adds one of those, it is that application's local convention, not Cratis; keep such conventions in the repository's own instructions, never present them as framework. The step-by-step workflow, the exact signatures and the traps live in the **cratis-arc-ef-core-migration** skill; this rule states the invariants.

## DbContext base types **[contract]**

Always derive from one of Arc's base types — never directly from `DbContext`:

- **`ReadOnlyDbContext`** — for every read model / projection context (the vast majority). Its save-changes interceptor **throws** on any `SaveChanges`, so a projection cannot be bypassed by accident.
- **`BaseDbContext`** — only for a writable context that genuinely owns state (device state, infrastructure state).

```csharp
public class StartupPhaseDbContext(DbContextOptions<StartupPhaseDbContext> options)
    : ReadOnlyDbContext(options)
{
    public DbSet<StartupPhase> StartupPhases => Set<StartupPhase>();
}
```

Use the primary constructor; expose each `DbSet<T>` as an expression-bodied property over `Set<T>()`. Co-locate the context with the feature whose read models it holds.

## Feature contexts — not god contexts **[convention]**

One focused `DbContext` per feature or tightly related feature group. Never aggregate unrelated entities into a single context: a context's shape is a cohesion boundary, and a god context couples every feature's schema change to every other's.

## State mutation — the golden rule **[convention]**

> **Never mutate state directly through a DbContext.**

All state changes flow through events and Chronicle projections; a direct write bypasses the event log and the audit trail. Only projection infrastructure, the Chronicle engine, and deliberate reference-data synchronization write through a context — and those are `BaseDbContext`s, never `ReadOnlyDbContext`s.

## Registration **[contract]**

Wire EF Core through the Arc builder and give it the connection string:

```csharp
builder.AddCratisArc(arc => arc.WithEntityFrameworkCore(
    options => options.ConnectionString = connectionString));
```

With a non-empty connection string and `AutoDiscoverDbContexts` (the default), Arc discovers every public `BaseDbContext` subtype not marked `[IgnoreAutoRegistration]`, registering `ReadOnlyDbContext` subtypes read-only and the rest read-write. ⚠️ `WithEntityFrameworkCore()` with **no** connection string discovers nothing. Explicit registration remains available (`AddDbContextWithConnectionString<T>`, `AddReadOnlyDbContextWithConnectionString<T>`, `AddReadModelDbContextsFromAssemblies`, `AddReadModelDbContextsWithConnectionStringFromAssemblies`).

Select the provider with **`UseDatabaseFromConnectionString(connectionString)`** — never hard-code `UseSqlite` / `UseNpgsql` / `UseSqlServer`. It infers `DatabaseType` (`Sqlite`, `SqlServer`, `PostgreSql`) from the connection string, ensures a SQLite file's directory exists, and installs Arc's migrations SQL generator for that provider (which is what adds the JSON validation constraint for `[Json]` columns). An unsupported connection string throws `UnsupportedDatabaseType`.

## Migrations **[contract]/[convention]**

Migrations are created and applied with the **standard EF Core tooling** (`dotnet ef migrations add`, `dotnet ef database update`, or `Database.Migrate()` at startup). Arc adds only the column helpers that keep one migration valid across all three providers — use them instead of raw `table.Column<T>()`:

| Helper | Use for |
| --- | --- |
| `table.StringColumn(migrationBuilder, …)` | text |
| `table.GuidColumn(migrationBuilder, …)` | GUID / UUID |
| `table.NumberColumn<T>(migrationBuilder, …)` | integers, longs, decimals |
| `table.BoolColumn(migrationBuilder, …)` | booleans |
| `table.DateTimeOffsetColumn(migrationBuilder, …)` | timestamps with offset |
| `table.AutoIncrementColumn(migrationBuilder, …)` | identity columns |
| `table.JsonColumn<TProperty>(migrationBuilder, …)` | a `[Json]`-mapped property |
| `migrationBuilder.Add{String,Guid,Number,Bool,DateTimeOffset,AutoIncrement,Json,Point,LineString,Polygon}Column(…)` | adding a column to an existing table |

Where a repository organizes its migrations (a dedicated project, a versioned file naming scheme, a table-name constants class) is its own convention: follow what the repository already does, and do not introduce one as if Arc required it.

## Concepts and JSON **[contract]**

`ConceptAs<T>` properties map to their underlying primitive without per-property value converters: `BaseDbContext.OnModelCreating` applies the concept conversions, and every Arc registration path calls `AddConceptAsSupport()` so concepts also work inside LINQ predicates. (A context you register with plain EF `AddDbContext` must call `options.AddConceptAsSupport()` itself.) Mark a property `[Json]` to store it as a JSON column with the provider's validation constraint.

## Observation **[contract]**

An EF Core read model can back an observable query: `dbSet.Observe(...)`, `ObserveById(...)` and `ObserveSingle(...)` (extension methods declared in the `Microsoft.EntityFrameworkCore` namespace, so they are in scope wherever the `DbSet` is) push changes through the provider's change notifier — see the skill for what each provider supports.

## See also

- the **cratis-arc-ef-core-migration** skill — wiring, making an EF read model injectable into a command, writing a migration, observing, and specs.
- [efcore.specs.md](./efcore.specs.md) — `DbContext` specs with SQLite in-memory.
