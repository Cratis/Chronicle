# Read Models

Read models in Chronicle represent the current state of your application derived from events stored in the event log. They provide a denormalized, queryable view of your data optimized for reading, making them essential for building responsive applications.

## What are Read Models?

A read model is a projection of events into a structured format that's optimized for querying. Unlike traditional databases where you store current state directly, Chronicle builds read models by applying events from the event log, ensuring your data is always in sync with what actually happened in your system.

Read models serve several purposes:

- **Query optimization**: Denormalized views designed for specific read patterns
- **Strong consistency**: Can be retrieved with the exact current state
- **Audit capability**: Can track how state evolved through event snapshots
- **Flexibility**: Multiple read models can be created from the same events

## How Read Models are Produced

Chronicle supports two primary mechanisms for producing read models, both accessible through the `IReadModels` API:

### Projections

Projections transform events into read models by defining how each event type affects the state. Chronicle supports two styles of projections:

**Model-Bound Projections** use attributes on the read model:

```csharp
public record AccountInfo(
    [Key] Guid Id,
    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))] string Name,
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [Add<MoneyDeposited>(nameof(MoneyDeposited.Amount))]
    [Subtract<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Balance);
```

**Declarative Projections** use a fluent API:

```csharp
public class AccountInfoProjection : IProjectionFor<AccountInfo>
{
    public ProjectionId Identifier => "AccountInfo";

    public void Define(IProjectionBuilderFor<AccountInfo> builder) => builder
        .From<AccountOpened>(_ => _
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.Balance).To(e => e.InitialBalance))
        .From<MoneyDeposited>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<MoneyWithdrawn>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));
}
```

Learn more about projections in the [Projections documentation](../projections/index.md).

### Reducers

Reducers provide an aggregate-based approach where all events for an aggregate root are reduced into a single state. They're ideal for maintaining entity state:

```csharp
public record Account(Guid Id, string Name, decimal Balance);

public class AccountReducer : IReducerFor<Account>
{
    public ReducerId Identifier => "Account";

    public Account Initial => new(Guid.Empty, string.Empty, 0m);

    public Account Reduce(Account current, object @event) => @event switch
    {
        AccountOpened e => current with { Id = e.Id, Name = e.Name, Balance = e.InitialBalance },
        MoneyDeposited e => current with { Balance = current.Balance + e.Amount },
        MoneyWithdrawn e => current with { Balance = current.Balance - e.Amount },
        _ => current
    };
}
```

Learn more about reducers in the [Reducers documentation](../recipes/reducers.md).

## The IReadModels API

The `IReadModels` interface provides a unified API for working with read models, regardless of whether they're produced by projections or reducers.

### Accessing IReadModels

The `IReadModels` interface is accessible through the `IEventStore`:

```csharp
public class AccountService
{
    readonly IEventStore _eventStore;

    public AccountService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<AccountInfo?> GetCurrentAccount(Guid accountId)
    {
        return await _eventStore.ReadModels.GetInstanceById<AccountInfo>(accountId);
    }
}
```

## Key Characteristics

### Strong Consistency

When you retrieve a read model using `GetInstanceById`, Chronicle ensures you get the most up-to-date state by applying all relevant events from the event log. This provides strong consistency guarantees.

### On-Demand Computation

Read models retrieved through `GetInstanceById` are computed on-demand by replaying events. This ensures accuracy but comes with performance considerations for read models with long event histories.

### Type Safety

The API provides full type safety through generic methods, ensuring compile-time checking when working with read models.

## Next Steps

- [Getting a Single Instance](getting-single-instance.md) - Learn how to retrieve a specific read model instance
- [Getting a Collection of Instances](getting-collection-instances.md) - Learn how to retrieve all instances of a read model
- [Getting Snapshots](getting-snapshots.md) - Understand how to retrieve historical snapshots of read model state
- [Watching Read Models](watching-read-models.md) - Learn how to observe real-time changes to read models
