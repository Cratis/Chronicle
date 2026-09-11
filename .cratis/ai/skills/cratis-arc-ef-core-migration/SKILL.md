---
name: cratis-arc-ef-core-migration
description: Change an Entity Framework Core schema in a Cratis Arc application — the DbContext base types Arc provides, the cross-database column helpers, JSON columns, how a migration is created and applied, and how an EF Core read model becomes injectable into a command. Use when adding or changing a table, column, relationship, or index behind an Arc application. Do not use for Chronicle read models or for query paging.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-ef-core-migration/SKILL.md -->

# Change an EF Core schema in an Arc application

Arc adds three things to Entity Framework Core: DbContext base types that apply
its conversions, cross-database column helpers for migrations, and a resolver
that makes an EF Core read model injectable into a command by the command's key.
Everything else is ordinary EF Core.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.EntityFrameworkCore` | `22.10.4` | `BaseDbContext`, `ReadOnlyDbContext`, `WithEntityFrameworkCore`, column helpers, `EntityFrameworkReadModelForCommandResolver` |
| `Cratis.Arc.Core` | `22.10.4` | `Cratis.Arc.Queries.ModelBound.ReadModelAttribute`, `ICanResolveReadModelForCommand` |
| `Microsoft.EntityFrameworkCore` | `10.0.11` | EF Core itself, including all migration tooling |

Reverify before claiming support for another version.

⚠️ **Arc ships no migration tooling of its own.** There is no design-time
DbContext factory, no migration runner, no table-name constant convention, and
nothing that reads or writes `__EFMigrationsHistory` anywhere in Arc. Migrations
are created and applied with the standard EF Core tooling. Guidance describing an
`ApplyAllMigrations` runner, a `WellKnownTables` class, or a mandatory
`Database`/`Core`/`Infrastructure` project split is describing **one
application's local convention**, not Arc.

## Pick the DbContext base type

| Base | Use for |
| --- | --- |
| `ReadOnlyDbContext` | Read models and projections — the vast majority |
| `BaseDbContext` | A context that genuinely owns writable state |

Both take `DbContextOptions` through a primary constructor; **neither has a
parameterless constructor**, so the derived context must pass options through:

```csharp
public class <Feature>DbContext(DbContextOptions<<Feature>DbContext> options)
    : ReadOnlyDbContext(options)
{
    public DbSet<<ReadModel>> <ReadModels> => Set<<ReadModel>>();
}
```

`BaseDbContext.OnModelCreating` applies Arc's JSON, `ConceptAs<T>` and `Guid`
conversions. Override `OnModelCreating` only after calling `base` — and do not
override `OnConfiguring`.

`ReadOnlyDbContext` throws `InvalidOperationException` from both `SaveChanges()`
and `SaveChangesAsync()`, and eagerly loads every navigation. Turn the eager
loading off by overriding `protected virtual bool IsEagerLoadingEnabled => false`.

Keep one focused context per feature rather than one context holding unrelated
entities.

## Wire it up

```csharp
builder.AddCratisArc(arc => arc.WithEntityFrameworkCore(
    options => options.ConnectionString = <connectionString>));
```

`WithEntityFrameworkCore(configureOptions?, configureEfCore?)` registers the
observation services and then, **only when `AutoDiscoverDbContexts` is true and
the connection string is non-empty**, discovers and registers every public
`BaseDbContext` subtype that is not marked `[IgnoreAutoRegistration]`.

⚠️ `WithEntityFrameworkCore()` with no arguments discovers **nothing**.
`EntityFrameworkCoreOptions.ConnectionString` defaults to the empty string, and
both discovery gates test it. A setup that looks wired and registers no context
is almost always this.

Discovery splits the contexts it finds: `ReadOnlyDbContext` subtypes are
registered read-only, everything else read-write. Registering by hand instead is
supported through `AddDbContextWithConnectionString<T>`,
`AddReadOnlyDbContextWithConnectionString<T>`,
`AddReadModelDbContextsFromAssemblies`, and
`AddReadModelDbContextsWithConnectionStringFromAssemblies` — note their
configuration callback is `Action<IServiceProvider, DbContextOptionsBuilder>`,
taking **two** parameters.

Use `UseDatabaseFromConnectionString(connectionString)` rather than hard-coding
`UseSqlite`/`UseNpgsql`/`UseSqlServer`. It picks the provider from the connection
string, creates a SQLite file's directory when needed, and installs the Arc
migrations SQL generator for that provider — which is what adds the JSON
validation constraint for `[Json]` columns. Supported values of `DatabaseType`
are `Sqlite`, `SqlServer` and `PostgreSql`.

## Making a read model injectable into a command

An EF Core entity becomes a command-injectable read model when **all** of these
hold:

1. the entity type carries `[ReadModel]` — `Cratis.Arc.Queries.ModelBound.ReadModelAttribute`,
   the same attribute a Chronicle read model uses; there is no EF-specific one;
2. it is exposed as a public `DbSet<T>` property;
3. that `DbSet<T>` is on a **`ReadOnlyDbContext`** subtype.

⚠️ Point 3 is the one that bites. A `[ReadModel]` entity on a plain
`BaseDbContext` is still registered and still queryable — it is simply **never**
contributed to command-side resolution, because only read-only contexts are
handed to it. Nothing warns.

The resolver declares `ReadModelForCommandOwnership.Declared`, so a `DbSet<T>`
carrying a read model claims that type even when another provider already
resolves it. Resolution loads the entity by its **single-property primary key**
from the command's resolved key; a composite key throws
`EntityDoesNotHavePrimaryKey`, and a command with no usable key raises
`UnableToResolveReadModelFromCommandContext`, which is a client-input failure
(HTTP 400) rather than a server fault.

`ARC0006` warns when such a parameter is non-nullable, because a command-scoped
read model can be missing. It is the only Arc diagnostic that touches EF Core
read models, and only because they share `[ReadModel]`.

## Write the migration

Use the standard EF Core tooling — `dotnet ef migrations add <Name>` and
`dotnet ef database update` — with the `Microsoft.EntityFrameworkCore.Design`
package referenced by the project that owns the context. Arc contributes nothing
to how a migration is created, discovered, or applied.

What Arc **does** contribute is a set of column helpers that pick the right
provider-specific SQL type, so one migration works across SQLite, SQL Server and
PostgreSQL. Use them instead of a raw `table.Column<T>()` with a hard-coded type.

On `ColumnsBuilder`, inside `CreateTable`:

```csharp
migrationBuilder.CreateTable(
    name: "<Table>",
    columns: table => new
    {
        Id = table.StringColumn(migrationBuilder, nullable: false),
        <Name> = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
        <Count> = table.NumberColumn<int>(migrationBuilder, nullable: true),
        <At> = table.DateTimeOffsetColumn(migrationBuilder),
        <Key> = table.GuidColumn(migrationBuilder),
    },
    constraints: table => table.PrimaryKey("PK_<Table>", x => x.Id));
```

`StringColumn`, `NumberColumn<T>`, `BoolColumn`, `AutoIncrementColumn`,
`GuidColumn` and `DateTimeOffsetColumn` all take the `MigrationBuilder` as their
first argument — that is how they learn which database they are generating for.
All of them default `nullable` to **`true`**, so a required column has to say
`nullable: false` explicitly.

On `MigrationBuilder`, for an existing table:

```csharp
migrationBuilder.AddStringColumn(
    name: "<Column>",
    table: "<Table>",
    maxLength: 1000,
    nullable: true);
```

`AddStringColumn`, `AddNumberColumn<T>`, `AddBoolColumn`,
`AddAutoIncrementColumn`, `AddGuidColumn`, `AddDateTimeOffsetColumn`,
`AddPointColumn`, `AddLineStringColumn` and `AddPolygonColumn` follow the same
`(name, table, …, schema)` shape. `NumberColumn<T>`/`AddNumberColumn<T>` are
constrained to `INumber<T>`.

For a JSON column use `table.JsonColumn<T>(migrationBuilder)` or
`migrationBuilder.AddJsonColumn<T>(...)`. Those annotate the operation with
`cratis:ColumnType = "json"`, which is what makes the provider's Arc migrations
generator emit the validation constraint — `json_valid` on SQLite,
`ISJSON(...) = 1` on SQL Server, a `jsonb` check plus a GIN index on PostgreSQL.
Mark the corresponding property `[Json]`.

`migrationBuilder.GetDatabaseType()` is available when a migration genuinely has
to branch per provider.

## Observing an EF Core read model

`DbSet<T>` gains `Observe(filter?, configure?)`, `ObserveSingle(filter?, configure?)`
and `ObserveById<TEntity, TId>(id, configure?)`, returning
`ISubject<IEnumerable<T>>` / `ISubject<T>`. Register the services with
`AddEntityFrameworkCoreObservation()` and `options.AddObservation(serviceProvider)`
on the context — `WithEntityFrameworkCore` already does the first for you.

Paging and sorting are applied at the source for these, and an out-of-range page
size is clamped rather than throwing, and an unknown sort field is ignored rather
than failing.

⚠️ Database-level change notification differs by provider: PostgreSQL uses
LISTEN/NOTIFY, SQL Server uses Service Broker, and **SQLite has none at all** —
its notifier is a no-op and only in-process changes are seen. Notification setup
failures are logged and degrade to in-process only rather than failing the app.

## Specify it

Arc ships no EF Core test harness. The framework's own EF Core specifications
use SQLite in-memory with `EnsureCreated()` for the fast path, and real
PostgreSQL and SQL Server containers for provider-specific behavior. The EF Core
in-memory provider is not used anywhere.

`EnsureCreated()` builds the schema from the model, **not** from the migrations,
so a specification suite that uses it will not notice a migration that does not
match the model. Cover the migration itself against a real provider when the
schema shape matters.

## Verify

- The context derives from `ReadOnlyDbContext` (or `BaseDbContext` when it truly
  writes) and passes `DbContextOptions` through.
- A read model intended for command injection carries `[ReadModel]`, is a
  `DbSet<T>`, and sits on a `ReadOnlyDbContext`.
- The read model has a single-property primary key.
- `WithEntityFrameworkCore` is given a non-empty connection string, or contexts
  are registered explicitly.
- No provider is hard-coded; `UseDatabaseFromConnectionString` is used.
- Every migration column goes through an Arc column helper, and required columns
  say `nullable: false`.
- JSON columns are `[Json]` on the model and `JsonColumn`/`AddJsonColumn` in the
  migration.
- The migration was produced and applied with the standard EF Core tooling.
- `dotnet build` is clean in Debug and Release and the specifications pass.

## Route near misses

- A Chronicle-backed read model or projection: the Chronicle read-model guidance.
- Server-side paging or sorting on a query: `cratis-arc-query-paging`.
- What an injected read model resolves to when it does not exist:
  `cratis-arc-command`.
