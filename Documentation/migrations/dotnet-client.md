# C# client usage

This guide covers how to declare event type migrations in a .NET client, the operations available to you, and what happens when your migrators are registered with the Chronicle Kernel.

## Prerequisites

- A Chronicle-enabled .NET application
- An event type marked with `[EventType]` that has evolved beyond generation 1

## Marking an event type with a generation

Every `[EventType]` that has evolved past its first version must declare its current generation. The generation is part of the event type identity:

```csharp
// Generation 1 (original) — no explicit generation needed, defaults to 1
[EventType]
public record AuthorRegistered(string Name);
```

When the schema changes, the new record carries the higher generation. The old record can be kept for documentation or removed — Chronicle identifies events by the type name, not the .NET class:

```csharp
// Generation 2 — Name has been split into FirstName and LastName
[EventType(2)]
public record AuthorRegistered(string FirstName, string LastName);
```

## Defining a migrator

Implement `IEventTypeMigrationFor<TEvent>` where `TEvent` is the **latest generation** of the event type. The interface requires:

- `From` — the generation this migrator reads from
- `To` — the generation this migrator produces
- `Upcast(IEventMigrationBuilder)` — transformation from `From` to `To`
- `Downcast(IEventMigrationBuilder)` — transformation from `To` back to `From`

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public class AuthorRegisteredMigrator : IEventTypeMigrationFor<AuthorRegistered>
{
    public EventTypeGeneration From => 1;
    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder)
    {
        builder.Properties(pb =>
        {
            var firstName = pb.Split("Name", separator: " ", part: SplitPartIndex.First);
            var lastName  = pb.Split("Name", separator: " ", part: SplitPartIndex.Second);
        });
    }

    public void Downcast(IEventMigrationBuilder builder)
    {
        builder.Properties(pb =>
        {
            var name = pb.Combine("FirstName", "LastName");
        });
    }
}
```

Migrators are discovered automatically at startup — no explicit registration is needed.

## Migration operations

All operations return a `PropertyExpression` that identifies the expression in the migration definition. The property name you assign to the returned expression in `Properties()` becomes the output property name in the transformed event.

### Split

Extracts one segment of a string property by splitting it on a separator.

```csharp
builder.Properties(pb =>
{
    var firstName = pb.Split("FullName", separator: " ", part: 0);  // first segment
    var lastName  = pb.Split("FullName", separator: " ", part: 1);  // second segment
});
```

Use `SplitPartIndex.First` and `SplitPartIndex.Second` for the most common cases:

```csharp
var firstName = pb.Split("FullName", " ", SplitPartIndex.First);
var lastName  = pb.Split("FullName", " ", SplitPartIndex.Second);
```

### Combine

Joins multiple source properties into a single string value, separated by a space.

```csharp
builder.Properties(pb =>
{
    var fullName = pb.Combine("FirstName", "LastName");
});
```

### RenamedFrom

Reads a property value from an old property name. Use this when a property is being renamed between generations.

```csharp
builder.Properties(pb =>
{
    var email = pb.RenamedFrom("EmailAddress");  // was EmailAddress, now Email
});
```

### DefaultValue

Provides a literal default value for a property that did not exist in the source generation.

```csharp
builder.Properties(pb =>
{
    var status = pb.DefaultValue("active");
    var retries = pb.DefaultValue(0);
    var enabled = pb.DefaultValue(true);
});
```

## Multi-generation migrations

If your event type spans more than two generations, define one migrator per generation pair. Chronicle chains the migrators automatically:

```csharp
// Generation 1 → 2 migrator
public class PersonRegisteredV1ToV2 : IEventTypeMigrationFor<PersonRegistered>
{
    public EventTypeGeneration From => 1;
    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            var email = pb.RenamedFrom("EmailAddress");
        });

    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            var emailAddress = pb.RenamedFrom("Email");
        });
}

// Generation 2 → 3 migrator
public class PersonRegisteredV2ToV3 : IEventTypeMigrationFor<PersonRegistered>
{
    public EventTypeGeneration From => 2;
    public EventTypeGeneration To => 3;

    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            var firstName = pb.Split("Name", " ", SplitPartIndex.First);
            var lastName  = pb.Split("Name", " ", SplitPartIndex.Second);
        });

    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            var name = pb.Combine("FirstName", "LastName");
        });
}
```

When a generation 1 event arrives, the Kernel chains the upcasts: 1→2, then 2→3, and stores all three generations.

## How registration works

When your application connects to Chronicle, the client:

1. Discovers all `IEventTypeMigrationFor<T>` implementations via `IClientArtifactsProvider`
2. Invokes `Upcast` and `Downcast` on each migrator to capture the transformation declarations
3. Converts the declarations into JmesPath expressions
4. Sends the complete `EventTypeDefinition` — including all generations and their migration definitions — to the Kernel during event type registration

From that point on, the Kernel applies the migrations autonomously on every event append, without any further involvement from the client.

## Validation: missing migrators

If an event type is declared with a generation higher than 1 but has no migrators covering all generations up to the current one, Chronicle throws `MissingEventTypeMigrators` during startup. This prevents silent data loss from an incomplete migration chain.

```text
Cratis.Chronicle.Events.Migrations.MissingEventTypeMigrators:
  Event type 'AuthorRegistered' is at generation 3 but no migrators are registered for it.
```

Ensure every generation gap has a corresponding `IEventTypeMigrationFor<T>` implementation before deploying an event type with a new generation.
