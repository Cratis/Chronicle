---
applyTo: "**/*.cs"
---

# Entity Framework Core Instructions

## Project Structure

Responsibilities are split across three projects:

| Project | Responsibility |
|---------|----------------|
| `Database` | Migrations only – no entities, no DbContexts |
| `Core` | Entities and feature DbContexts (co-located with features) |
| `Infrastructure` | DbContext registration, migration runner, cross-cutting EF setup |

**Critical dependency rule**: `Database` must NEVER import from `Core`. Migrations reference only `WellKnownTables` constants (strings) and EF migration types. The dependency chain is:

```
Core → Infrastructure → Database
```

## DbContext Base Types

Always use the Cratis Arc base types — never inherit directly from `DbContext`:

- **`ReadOnlyDbContext`** — for all read model / projection contexts (the vast majority)
- **`BaseDbContext`** — only for writable contexts that own state (e.g. device state, infrastructure state)

```csharp
// ✅ Read model context
public class StartupPhaseDbContext(DbContextOptions<StartupPhaseDbContext> options)
    : ReadOnlyDbContext(options)
{
    public DbSet<StartupPhase> StartupPhases => Set<StartupPhase>();
    public DbSet<PersonnelAssignment> PersonnelAssignments => Set<PersonnelAssignment>();
}

// ✅ Writable (state-owning) context
public class DeviceStateDbContext(DbContextOptions<DeviceStateDbContext> options)
    : BaseDbContext(options)
{
    public DbSet<DeviceState> DeviceStates => Set<DeviceState>();
}
```

Use the primary constructor pattern. Expose `DbSet<T>` as expression-bodied properties using `Set<T>()`.

## Feature Contexts — Not God Contexts

Create one focused DbContext per feature or tightly-related feature group. Never aggregate unrelated entities into a single context.

```csharp
// ❌ God context
public class AppDbContext : DbContext
{
    public DbSet<Mission> Missions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Station> Stations { get; set; }
    // ... many more
}

// ✅ Focused feature context
public class StartupPhaseDbContext(DbContextOptions<StartupPhaseDbContext> options)
    : ReadOnlyDbContext(options)
{
    public DbSet<StartupPhase> StartupPhases => Set<StartupPhase>();
    public DbSet<PersonnelAssignment> PersonnelAssignments => Set<PersonnelAssignment>();
}
```

Co-locate the DbContext file with its feature:

```
Missions/Ongoing/StartupPhase/
├── StartupPhase.cs
├── StartupPhaseDbContext.cs
└── ...
```

## State Mutation — The Golden Rule

> **Never mutate state directly through a DbContext.**

All state changes must flow through events and Chronicle projections. Direct writes bypass the audit trail and event log.

```csharp
// ❌ Direct mutation — forbidden
dbContext.StartupPhases.Add(new StartupPhase(...));
await dbContext.SaveChangesAsync();

// ✅ Correct: emit an event, let the projection handle writes
[Command]
public record UpdateStartupPhase(MissionId MissionId, ...) { ... }
```

Only infrastructure projection code, the Chronicle event engine, and reference data sync may write through DbContexts.

## Registration

Use the Cratis Arc `Cratis.Arc.EntityFrameworkCore` extension methods:

```csharp
// Register a single writable DbContext
services.AddDbContextWithConnectionString<DeviceStateDbContext>(connectionString, optionalConfigure);

// Auto-discover and register ALL ReadOnlyDbContext subtypes from given assemblies
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(
    connectionString,
    configureOptions,
    [Assembly.GetExecutingAssembly()]);
```

Configure the database provider using `UseDatabaseFromConnectionString`, which auto-detects PostgreSQL vs SQLite from the connection string:

```csharp
options.UseDatabaseFromConnectionString(connectionString);
```

Centralise all DbContext setup in a single `AddApplicationDbContexts` extension method per layer.

## Multiple Database Support

The application supports both PostgreSQL (ASP.NET mode) and SQLite (MAUI mode) from the same code. The provider is selected at runtime via the connection string — `UseDatabaseFromConnectionString` handles the detection.

Never hardcode a provider (e.g. `UseSqlite` or `UseNpgsql`) in application code. Always use `UseDatabaseFromConnectionString`.

## Migrations

Migrations live exclusively in the **`Database`** project, never in `Core` or `Infrastructure`.

### Organisation

Each entity category has its own folder with versioned migration files:

```
Database/
├── Missions/
│   ├── v1_0_0.cs
│   └── v1_1_0.cs
├── Users/
│   └── v1_0_0.cs
└── WellKnownTables.cs
```

### Naming

Version files using the pattern `v{major}_{minor}_{patch}.cs` and place them inside a namespace matching their folder:

```csharp
namespace Database.Missions;

public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { ... }
    protected override void Down(MigrationBuilder migrationBuilder) { ... }
}
```

The migration ID is composed as `{folder}_{ClassName}` (e.g. `Missions_v1_0_0`).

### Cross-Database Column Helpers

Always use the Cratis Arc `MigrationBuilder` extension helpers to define columns. These abstract over PostgreSQL and SQLite type differences:

| Helper | Use for |
|--------|---------|
| `table.StringColumn(migrationBuilder)` | Text / varchar columns |
| `table.GuidColumn(migrationBuilder)` | UUID / GUID columns |
| `table.NumberColumn<T>(migrationBuilder)` | Integer, long, or numeric columns |
| `table.DateTimeOffsetColumn(migrationBuilder)` | Timestamps with timezone |

Never use raw EF `table.Column<string>()` etc. — the helpers ensure cross-database compatibility.

```csharp
// ✅ Cross-database migration
migrationBuilder.CreateTable(
    name: WellKnownTables.Missions,
    columns: table => new
    {
        Id = table.StringColumn(migrationBuilder, nullable: false),
        Title = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
        ResourceId = table.NumberColumn<int>(migrationBuilder, nullable: true),
        DispatchTime = table.DateTimeOffsetColumn(migrationBuilder),
        UrgencyId = table.GuidColumn(migrationBuilder)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Missions", x => x.Id);
        table.ForeignKey("FK_Missions_Urgency", x => x.UrgencyId,
            WellKnownTables.MissionUrgencies, "Id", onDelete: ReferentialAction.SetNull);
    });
```

### Table Names — WellKnownTables

Always reference table names from `WellKnownTables` constants. Never use magic strings directly in migrations.

```csharp
// ❌ Magic string
migrationBuilder.CreateTable(name: "Missions", ...);

// ✅ Constant
migrationBuilder.CreateTable(name: WellKnownTables.Missions, ...);
```

Add new table names to `Database/WellKnownTables.cs` before writing the migration.

### Applying Migrations

Migrations are applied via a custom runner from the `Database` project (not `dotnet ef database update`):

```csharp
// ASP.NET mode
await app.ApplyAllMigrations(connectionString);

// MAUI / IServiceProvider mode
await services.ApplyAllMigrations(connectionString);
```

The runner discovers all `Migration` subclasses from the `Database` assembly, checks the EF history table, and applies pending migrations in version order within a transaction. Both PostgreSQL and SQLite are supported through the same runner.

## Auto-Discovery

The Cratis Arc `IImplementationsOf<T>` mechanism discovers types at runtime:

- `IImplementationsOf<BaseDbContext>` — all DbContext subtypes across all loaded assemblies
- `.NotReadonly()` extension — filters out `ReadOnlyDbContext` subtypes to isolate writable contexts
- `IReadModelDbContexts.GetAll()` — returns all registered `ReadOnlyDbContext` instances
