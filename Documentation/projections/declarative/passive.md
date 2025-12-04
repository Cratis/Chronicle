# Passive projection

A passive projection is a projection that is not actively materialized to a persistent store but can be queried on-demand for in-memory lookups. This is useful for scenarios where you need to construct read models from events without the overhead of maintaining persistent state.

## Defining a passive projection

Use the `.Passive()` method to mark a projection as passive:

```csharp
using Cratis.Chronicle.Projections;

public class UserSummaryProjection : IProjectionFor<UserSummary>
{
    public void Define(IProjectionBuilderFor<UserSummary> builder) => builder
        .Passive()
        .AutoMap()
        .From<UserCreated>()
        .From<UserUpdated>();
}
```

This projection:

- Will not be actively maintained in persistent storage
- Can be queried on-demand using the `IProjections` service
- Reconstructs the read model from events when requested

## Using passive projections

Passive projections are accessed through the `IProjections` interface using the `GetInstanceById` method:

```csharp
public class UserService
{
    private readonly IProjections _projections;

    public UserService(IProjections projections)
    {
        _projections = projections;
    }

    public async Task<UserSummary?> GetUserSummaryAsync(string userId)
    {
        var result = await _projections.GetInstanceById<UserSummary>(userId);
        return result.Model;
    }
}
```

## Read model definition

The read model is defined the same way as for regular projections:

```csharp
public record UserSummary(
    string Name,
    string Email,
    int LoginCount,
    DateTimeOffset LastLoginAt);
```

## Event definitions

Events should match the read model structure or use explicit mapping:

```csharp
[EventType]
public record UserCreated(string Name, string Email);

[EventType]
public record UserUpdated(string Name, string Email);

[EventType]
public record UserLoggedIn(DateTimeOffset LoginTime);
```

## How it works

When you call `GetInstanceById` on a passive projection:

1. Chronicle retrieves all relevant events for the specified event source ID
2. The projection logic is applied to reconstruct the read model in memory
3. The resulting read model is returned without being persisted
4. Each call reconstructs the model from scratch, ensuring up-to-date data

## When to use passive projections

Passive projections are ideal for:

- **Infrequent queries**: When read models are accessed rarely or sporadically
- **Real-time data**: When you always need the most current state without caching concerns
- **Memory-sensitive scenarios**: When you want to avoid storing projection state
- **Temporary calculations**: For read models that are computed and discarded
- **Testing and debugging**: When you need to inspect event-driven state without persistence

## Performance considerations

- Passive projections have higher latency since they reconstruct on each request
- They consume more CPU but less storage compared to active projections
- Consider caching strategies if the same passive projection is accessed frequently
- Use for read models with simple event processing logic to minimize reconstruction time
