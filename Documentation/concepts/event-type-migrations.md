# Event Type Migrations

Event type migrations enable you to evolve your event schemas over time while maintaining
compatibility with existing events. When an event type changes, you can define **upcasters**
and **downcasters** that automatically transform events between different generations.

## Why Migrations?

In evolving systems, event schemas naturally change:
- Properties are added or removed
- Properties are renamed
- Complex properties are split or combined

Chronicle's migration system allows you to:
1. Define declarative transformation rules
2. Automatically store all generations of an event when appending
3. Read events in any generation format

## Defining Migrations

To define a migration, implement the `IEventTypeMigrationFor<TEvent>` interface:

```csharp
public record AuthorRegisteredV1(string Name);

[EventType("author-registered", 2)]
public record AuthorRegistered(string FirstName, string LastName);

public class AuthorRegisteredMigrator : IEventTypeMigrationFor<AuthorRegistered>
{
    public EventTypeGeneration From => 1;
    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder)
    {
        builder.Properties(pb =>
        {
            pb.Split("Name", " ", 0);   // FirstName from first part of Name
            pb.Split("Name", " ", 1);   // LastName from second part of Name
        });
    }

    public void Downcast(IEventMigrationBuilder builder)
    {
        builder.Properties(pb =>
        {
            pb.Combine("FirstName", "LastName");  // Combine back to Name
        });
    }
}
```

## Migration Operations

The migration builder supports the following operations:

### Split

Splits a source property into parts using a separator:

```csharp
pb.Split("FullName", " ", 0);  // Gets first part
pb.Split("FullName", " ", 1);  // Gets second part
```

### Combine

Combines multiple properties into a single value:

```csharp
pb.Combine("FirstName", "LastName");  // Joins with space separator
```

### Rename

Renames a property from a previous name:

```csharp
pb.RenamedFrom("OldPropertyName");
```

### Default Value

Sets a default value for a new property:

```csharp
pb.DefaultValue(42);
pb.DefaultValue("default string");
```

## How Migrations Work

When an event is appended to the event store:

1. Chronicle identifies the event's current generation
2. The migration system retrieves all registered migrations for the event type
3. **Upcasting**: If there are higher generations, the event is transformed upward (1→2→3)
4. **Downcasting**: If there are lower generations, the event is transformed downward (3→2→1)
5. All generations are stored in the event sequence

This ensures that:
- Older consumers can still read events in their expected format
- Newer consumers can read events with the latest schema
- No data is lost during schema evolution

## Registration

Migrations are automatically discovered and registered when you connect to Chronicle.
Simply implement `IEventTypeMigrationFor<TEvent>` in your client application, and
Chronicle will:

1. Discover all migrators via `IClientArtifactsProvider`
2. Build migration definitions with JmesPath transformations
3. Send the definitions to the kernel during event type registration

## Best Practices

1. **Incremental generations**: Always migrate between consecutive generations (1→2, 2→3, not 1→3)
2. **Reversible transformations**: Ensure downcast can recreate the original structure where possible
3. **Default values**: Use `DefaultValue()` for new properties that didn't exist in older generations
4. **Test migrations**: Verify both upcast and downcast transformations work correctly
5. **Document changes**: Keep track of what changed between generations in your event types
