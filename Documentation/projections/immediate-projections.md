# Immediate Projections

Immediate projections provide a way to get strongly consistent read models without the need to materialize them to a database. Instead of relying on eventual consistency, immediate projections apply events in real-time to generate a read model instance on-demand. This ensures you get the most up-to-date state following strong consistency principles.

## How it works

When you call `GetInstanceById<TReadModel>()` on the `IProjections` API, the system:

1. Identifies the projection definition for the requested read model type
2. Retrieves all relevant events from the event store for the specified key
3. Applies the projection logic to these events in memory
4. Returns the resulting read model instance

The key benefit is that you get a projection result that reflects the exact current state without waiting for eventual consistency to materialize changes to a persistent store.

## Usage with Model-Bound Projections

For model-bound projections, you define your projection using attributes directly on the read model:

```csharp
public record AccountInfo(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [Add<MoneyDeposited>(nameof(MoneyDeposited.Amount))]
    [Subtract<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))]
    decimal Balance);
```

To get an immediate projection instance:

```csharp
public class AccountService
{
    readonly IProjections _projections;

    public AccountService(IProjections projections)
    {
        _projections = projections;
    }

    public async Task<AccountInfo?> GetAccountInfo(Guid accountId)
    {
        var result = await _projections.GetInstanceById<AccountInfo>(accountId);
        return result.Model;
    }
}
```

## Usage with Declarative Projections

For declarative projections, you implement `IProjectionFor<T>`:

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

public record AccountInfo(Guid Id, string Name, decimal Balance);
```

The usage is identical:

```csharp
public async Task<AccountInfo?> GetAccountInfo(Guid accountId)
{
    var result = await _projections.GetInstanceById<AccountInfo>(accountId);
    return result.Model;
}
```

## Key Considerations

### Performance

- Immediate projections are computed on-demand, which means they involve reading and processing events each time
- For frequently accessed read models with long event histories, consider using materialized projections instead
- Best suited for scenarios where strong consistency is more important than query performance

### Use Cases

- Financial transactions where you need the exact current balance
- Real-time dashboards that must show the latest state
- Scenarios where eventual consistency delays are unacceptable
- Read models that are accessed infrequently but require absolute accuracy

### Key Mapping

The key used in `GetInstanceById<TReadModel>()` typically corresponds to:

- The `[Key]` property in model-bound projections
- The key defined in the projection's `KeyFromEventSourceId` or similar configuration in declarative projections
- Often matches the Event Source ID or a derived identifier from your events
