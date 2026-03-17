# C# client usage

This guide covers how to declare event type migrations in a .NET client, the operations available to you, and what happens when your migrators are registered with the Chronicle Kernel.

## Prerequisites

- A Chronicle-enabled .NET application
- An event type marked with `[EventType]` that has evolved beyond generation 1

## Marking event types with generations

Every `[EventType]` that has evolved past its first version must declare its current generation. You keep both the old and new record types in your codebase — Chronicle identifies them by the shared event type identifier, not the C# class name.

```csharp
// Generation 1 (original) — no explicit generation needed, defaults to 1
[EventType]
public record AuthorRegisteredV1(string Name);

// Generation 2 — Name has been split into FirstName and LastName
[EventType(2)]
public record AuthorRegistered(string FirstName, string LastName);
```

Both records carry the same event type identifier (derived from the type name base). The generation number is what tells Chronicle how to route migrations.

## Defining a migrator

Extend `EventTypeMigration<TUpgrade, TPrevious>` where `TUpgrade` is the **newer** generation and `TPrevious` is the **older** one:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public class AuthorRegisteredMigration : EventTypeMigration<AuthorRegistered, AuthorRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<AuthorRegistered, AuthorRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(e => e.FirstName, s => s.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(e => e.LastName,  s => s.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<AuthorRegisteredV1, AuthorRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(e => e.Name, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
}
```

The `From` and `To` generation numbers are read automatically from the `[EventType]` attributes on `TPrevious` and `TUpgrade`. You do not declare them yourself, and the base class validates at construction time that `To == From + 1`, preventing accidental generation gaps.

Migrators are discovered automatically at startup — no explicit registration is needed.

## Migration operations

All operations are called on the property builder inside `builder.Properties(pb => ...)`. Each call declares one output property using a target property expression and one or more source property expressions drawn from the opposite generation's record.

### Split

Extracts one segment of a string property by splitting on a separator and taking the part at a given index.

```csharp
builder.Properties(pb => pb
    .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
    .Split(t => t.LastName,  s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second));
```

`PropertySeparator.Space` is a built-in constant. Any string is also implicitly convertible to a `PropertySeparator`, so `":"` works directly for colon-delimited fields. `SplitPartIndex.First` (index 0) and `SplitPartIndex.Second` (index 1) cover the most common cases; pass any `int` for deeper splits.

### Combine

Concatenates multiple source properties into a single string target property, joining with a separator. The second argument is a `PropertySeparator` — use the built-in `PropertySeparator.Space` or pass any string (e.g. `":"`).

```csharp
builder.Properties(pb => pb
    .Combine(t => t.FullName, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
```

### RenamedFrom

Maps a property from its old name in the source generation to its new name in the target generation.

```csharp
builder.Properties(pb => pb
    .RenamedFrom(t => t.Email, s => s.EmailAddress));
```

### DefaultValue

Provides a literal default for a property that did not exist in the source generation. Chronicle applies this value to any event stored before the property was introduced.

```csharp
builder.Properties(pb => pb
    .DefaultValue(t => t.Status, "active")
    .DefaultValue(t => t.RetryCount, 0)
    .DefaultValue(t => t.Enabled, true));
```

## Multi-generation migrations

If your event type spans more than two generations, define one migrator per generation pair. Chronicle chains them automatically.

```csharp
// Generation 1 → 2: rename Email
public class PersonRegisteredV1ToV2 : EventTypeMigration<PersonRegisteredV2, PersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<PersonRegisteredV2, PersonRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.Email, s => s.EmailAddress));

    public override void Downcast(IEventMigrationBuilder<PersonRegisteredV1, PersonRegisteredV2> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.EmailAddress, s => s.Email));
}

// Generation 2 → 3: split Name into FirstName / LastName
public class PersonRegisteredV2ToV3 : EventTypeMigration<PersonRegistered, PersonRegisteredV2>
{
    public override void Upcast(IEventMigrationBuilder<PersonRegistered, PersonRegisteredV2> builder) =>
        builder.Properties(pb => pb
            .Split(t => t.FirstName, s => s.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(t => t.LastName,  s => s.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<PersonRegisteredV2, PersonRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(t => t.Name, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
}
```

When a generation 1 event arrives, the Kernel chains the upcasts: 1→2, then 2→3, and stores all three generations.

## How registration works

When your application connects to Chronicle, the client:

1. Discovers all `EventTypeMigration<TUpgrade, TPrevious>` implementations via `IClientArtifactsProvider`
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

Ensure every generation gap has a corresponding `EventTypeMigration<TUpgrade, TPrevious>` subclass before deploying an event type with a new generation.
