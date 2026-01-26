# Simple projection

A projection in Cratis is a way to create read models from events in the event store. The simplest projection automatically maps event properties to read model properties with matching names using the default AutoMap behavior.

## Defining a simple projection

A projection implements `IProjectionFor<TReadModel>` and defines its behavior in the `Define` method:

```csharp
using Cratis.Chronicle.Projections;

public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>();
}
```

This projection:

- Automatically maps properties from `UserCreated` event to the `User` read model (AutoMap is enabled by default)
- Uses the event source ID as the key for the read model
- Properties with matching names are automatically mapped

> **Note:** AutoMap is enabled by default at the top level. You don't need to call `.AutoMap()` explicitly unless you want to ensure it's enabled in contexts where it might be disabled. Read more about [auto mapping](./auto-map.md)

## Read model definition

The read model is a simple record or class representing the projected data:

```csharp
public record User(string Name, string Email, DateTimeOffset CreatedAt);
```

## Event definition

The event should match the read model structure for auto-mapping to work:

```csharp
[EventType]
public record UserCreated(string Name, string Email, DateTimeOffset CreatedAt);
```

## How it works

When a `UserCreated` event is appended to the event log:

1. The projection automatically creates or updates a `User` read model
2. The event source ID becomes the key for the read model
3. Properties with matching names are copied from event to read model
4. The read model is stored and can be queried

Auto-mapping works when property names and types match between the event and read model. For more control over the mapping, see [projection with custom properties](./set-properties.md).
