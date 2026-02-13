# Passive Reducers

Passive reducers are registered with the kernel but do not actively observe and materialize events through the sink. This is useful when you want to compute read model state on-demand rather than maintaining it continuously.

## Understanding Passive vs Active Reducers

### Active Reducers (Default)

Active reducers:

- Automatically observe new events as they are appended
- Continuously maintain up-to-date read model state
- Persist state through the configured sink (typically MongoDB)
- Are immediately available for querying without computation delay

### What Makes a Reducer Passive?

Passive reducers:

- Are registered but do not actively observe events
- Compute state on-demand when requested
- Do not persist state through the sink
- Are ideal for ad-hoc queries or temporary computations

## Making a Reducer Passive

There are two ways to make a reducer passive:

### Option 1: Using the Reducer Attribute

Set the `isActive` parameter to `false` on the reducer class:

```csharp
[Reducer(isActive: false)]
public class TemporaryAnalyticsReducer : IReducerFor<Analytics>
{
    public Analytics OnDataRecorded(DataRecorded @event, Analytics? current, EventContext context)
    {
        var count = current?.RecordCount ?? 0;
        var sum = current?.TotalValue ?? 0m;

        return new Analytics(count + 1, sum + @event.Value, context.Occurred);
    }
}
```

### Option 2: Using the Passive Attribute on the Read Model

Mark the read model class with the `[Passive]` attribute:

```csharp
using Cratis.Chronicle.ReadModels;

[Passive]
public record AdHocReport(
    decimal TotalRevenue,
    int TransactionCount,
    DateTimeOffset GeneratedAt);

public class AdHocReportReducer : IReducerFor<AdHocReport>
{
    public AdHocReport OnTransactionCompleted(TransactionCompleted @event, AdHocReport? current, EventContext context)
    {
        var revenue = current?.TotalRevenue ?? 0m;
        var count = current?.TransactionCount ?? 0;

        return new AdHocReport(
            revenue + @event.Amount,
            count + 1,
            context.Occurred);
    }
}
```

**Note:** The `[Passive]` attribute on the read model automatically makes all reducers targeting that read model passive, regardless of individual reducer settings.

## Attribute Priority

When determining if a reducer is passive:

1. If the read model has `[Passive]` attribute → **Reducer is passive**
2. If the reducer has `[Reducer(isActive: false)]` → **Reducer is passive**
3. Otherwise → **Reducer is active** (default)

This means the `[Passive]` attribute on the read model takes precedence over the reducer's `isActive` setting.

## Use Cases for Passive Reducers

### 1. On-Demand Reports

Generate reports only when requested, avoiding the overhead of continuous state maintenance:

```csharp
[Passive]
public record MonthlyRevenueReport(
    decimal TotalRevenue,
    Dictionary<string, decimal> RevenueByCategory,
    int Month,
    int Year);

public class MonthlyRevenueReportReducer : IReducerFor<MonthlyRevenueReport>
{
    public MonthlyRevenueReport OnPaymentReceived(PaymentReceived @event, MonthlyRevenueReport? current, EventContext context)
    {
        var revenue = current?.TotalRevenue ?? 0m;
        var byCategory = current?.RevenueByCategory ?? new Dictionary<string, decimal>();

        if (!byCategory.ContainsKey(@event.Category))
            byCategory[@event.Category] = 0;

        byCategory[@event.Category] += @event.Amount;

        return new MonthlyRevenueReport(
            revenue + @event.Amount,
            byCategory,
            context.Occurred.Month,
            context.Occurred.Year);
    }
}
```

### 2. Temporary Analysis

Perform exploratory data analysis without polluting your persistent storage:

```csharp
[Passive]
public record CustomerBehaviorAnalysis(
    int UniqueCustomers,
    decimal AverageOrderValue,
    Dictionary<int, int> OrdersByHour);
```

### 3. Historical Snapshots

Compute read model state at specific points in time without maintaining continuous state:

```csharp
public class HistoricalBalanceService
{
    readonly IReducers _reducers;

    public async Task<AccountBalance> GetBalanceAtDate(Guid accountId, DateTimeOffset date)
    {
        // Passive reducer computes state on-demand from historical events
        var result = await _reducers.GetInstanceById<AccountBalance>(accountId);
        return result.ReadModel;
    }
}
```

### 4. Development and Testing

During development, you might want to register reducers without activating them:

```csharp
#if DEBUG
[Reducer(isActive: false)]
#endif
public class ExperimentalMetricsReducer : IReducerFor<ExperimentalMetrics>
{
    // Development/testing reducer
}
```

## Retrieving State from Passive Reducers

Passive reducers compute state on-demand using the same API as active reducers:

```csharp
public class ReportingService
{
    readonly IReducers _reducers;

    public ReportingService(IReducers reducers)
    {
        _reducers = reducers;
    }

    public async Task<MonthlyRevenueReport> GenerateReport(Guid reportId)
    {
        // This triggers the passive reducer to compute state from events
        var result = await _reducers.GetInstanceById<MonthlyRevenueReport>(reportId);
        return result.ReadModel;
    }
}
```

## Performance Considerations

### Trade-offs

**Active Reducers:**

- ✅ Fast queries (state is pre-computed)
- ✅ Always up-to-date
- ❌ Continuous resource usage
- ❌ Storage overhead

**Passive Reducers:**

- ✅ No continuous resource usage
- ✅ No storage overhead
- ❌ Slower queries (computed on-demand)
- ❌ Not pre-computed

### Best Practices

1. **Use active reducers for frequently accessed data** - When read model state is queried often
2. **Use passive reducers for infrequent queries** - When data is requested occasionally
3. **Consider caching** - For passive reducers with expensive computations
4. **Monitor performance** - Track computation time for passive reducers
5. **Event volume matters** - Passive reducers process all events each time; large event streams may be slow

## Switching Between Active and Passive

You can change a reducer from active to passive (or vice versa) by updating the attribute:

```csharp
// Was active, now passive
[Reducer(isActive: false)]
public class MyReducer : IReducerFor<MyReadModel>
{
    // Implementation
}
```

When you change the `isActive` setting:

- The reducer definition is updated in the kernel
- Active reducers will unsubscribe from the event stream
- Passive reducers will not start observing
- Persisted state (for previously active reducers) remains in storage but won't be updated

## Summary

Passive reducers provide flexibility in how you manage read model state:

- Use `[Reducer(isActive: false)]` to make a specific reducer passive
- Use `[Passive]` on the read model to make all reducers for that model passive
- The `[Passive]` attribute takes precedence over individual reducer settings
- Passive reducers are ideal for on-demand computations, reports, and analysis
- Choose active vs. passive based on access frequency and resource constraints
