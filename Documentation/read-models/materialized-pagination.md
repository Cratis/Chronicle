---
uid: Chronicle.ReadModels.Materialized
---

# Materialized Read Models

When a projection or reducer processes an event, Chronicle persists the resulting read model instance in a *sink* — a database-backed store that holds the materialized state. The `Materialized` API gives you direct, paginated access to that stored state without replaying the event log.

> [!IMPORTANT]
> The `Materialized` API is **not** a substitute for working directly with the underlying database. It provides a simple, database-agnostic way to retrieve and observe stored read model instances using skip/take pagination. For advanced queries — filtering, sorting, aggregation, full-text search, joins — use the sink's native query capabilities (MongoDB queries, SQL queries, etc.) directly.

## What Materialized Access Covers

The `Materialized` API is intentionally narrow. It answers one question well: *give me a page of instances I already know are stored in a sink*. This keeps it:

- **Database-agnostic** — the same call works regardless of whether the sink is MongoDB, SQL, or any future backend
- **Simple to use** — two methods, optional skip/take, and sensible defaults
- **Safe for large datasets** — only the requested page is loaded, never the full collection

What it does **not** cover:

- Filtering by field value
- Sorting by any property
- Aggregation or count queries
- Full-text or range searches
- Complex joins or projections across collections

For those needs, inject the sink's native client directly. If your sink is MongoDB, inject `IMongoCollection<TReadModel>`. If it is SQL, inject your `DbContext`. Those tools are purpose-built for complex queries and Chronicle does not try to replace them.

## Accessing the API

`IMaterializedReadModels` is exposed through `IReadModels.Materialized`:

```csharp
// Inject IEventStore, then reach through to the Materialized API
var instances = await eventStore.ReadModels.Materialized.GetInstances<Order>();
```

## Getting Instances

### Basic Usage

Retrieve the first page of stored instances using the defaults (skip: 0, take: 50):

```csharp
var instances = await eventStore.ReadModels.Materialized.GetInstances<Order>();
```

### Pagination

Both `skip` and `take` are optional with sensible defaults. Use them for page-based or offset-based navigation:

```csharp
// First page of 20
var page1 = await eventStore.ReadModels.Materialized.GetInstances<Order>(take: 20);

// Second page of 20
var page2 = await eventStore.ReadModels.Materialized.GetInstances<Order>(skip: 20, take: 20);

// Third page of 20
var page3 = await eventStore.ReadModels.Materialized.GetInstances<Order>(skip: 40, take: 20);
```

### Pagination Parameters

The parameters use strongly-typed concepts that convert implicitly from `int`:

| Parameter | Type | Default | Named Constants |
|---|---|---|---|
| `skip` | `InstanceCountToSkip?` | `0` | `InstanceCountToSkip.Zero` |
| `take` | `InstanceCount?` | `50` | `InstanceCount.Default`, `InstanceCount.Unlimited` |

```csharp
// Using named constants
var instances = await eventStore.ReadModels.Materialized.GetInstances<Order>(
    skip: InstanceCountToSkip.Zero,
    take: InstanceCount.Default);
```

### Building a Paged API Endpoint

```csharp
[HttpGet]
public async Task<IEnumerable<Order>> GetOrders(
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 20)
{
    return await _eventStore.ReadModels.Materialized.GetInstances<Order>(
        skip: page * pageSize,
        take: pageSize);
}
```

## Observing Changes

`ObserveInstances` returns an `IObservable<IEnumerable<TReadModel>>` that emits a new page snapshot whenever the underlying stored data changes. This is useful for live-updating UIs, dashboards, and monitoring tools.

```csharp
var subscription = eventStore.ReadModels.Materialized
    .ObserveInstances<Product>(take: 50)
    .Subscribe(products =>
    {
        // Called whenever the stored instances change
        Console.WriteLine($"Products updated: {products.Count()} in view");
    });

// Dispose when done to release the change stream
subscription.Dispose();
```

Observation relies on the sink's change stream mechanism:

- **MongoDB** — uses native MongoDB change streams
- **SQL** — uses polling-based change detection via `DbContext`

> [!NOTE]
> Always dispose observation subscriptions when the consumer is torn down to prevent resource leaks and keep change streams closed.

### Observing in a Service

```csharp
public class ProductDashboard : IDisposable
{
    readonly IDisposable _subscription;

    public ProductDashboard(IEventStore eventStore)
    {
        _subscription = eventStore.ReadModels.Materialized
            .ObserveInstances<Product>(take: 100)
            .Subscribe(UpdateView);
    }

    void UpdateView(IEnumerable<Product> products) { /* ... */ }

    public void Dispose() => _subscription.Dispose();
}
```

## When to Use the Materialized API

Use `Materialized.GetInstances` and `ObserveInstances` when:

- You need a page of stored read model instances for a list view, data grid, or infinite scroll UI
- You want real-time updates pushed to a connected UI without polling
- The dataset is large and loading everything into memory via event replay would be too slow or too expensive

Do **not** use the `Materialized` API when:

- You need to filter by a specific field — query the sink directly
- You need instances sorted by a property — query the sink directly
- You need a count of matching records — query the sink directly
- You need to run an aggregation — query the sink directly

## Comparison with On-Demand GetInstances

| | `ReadModels.GetInstances<T>()` | `ReadModels.Materialized.GetInstances<T>()` |
|---|---|---|
| **Data source** | Event log replay | Materialized sink (database) |
| **Consistency** | Strong — always current | Eventual — milliseconds behind |
| **Performance** | Proportional to event history | O(1) — direct database lookup |
| **Pagination** | No | Yes — skip/take |
| **Large datasets** | Slow — replays all events | Fast — loads only the requested page |
| **Filtering/sorting** | Post-fetch with LINQ | Not supported — query the sink directly |

## Related Topics

- [Consistency Models](consistency.md) — Understanding strong vs. eventual consistency
- [Getting a Single Instance](getting-single-instance.md) — On-demand computation for a single instance
- [Getting a Collection of Instances](getting-collection-instances.md) — On-demand collection retrieval via event replay
- [Watching Read Models](watching-read-models.md) — Observe event-log-sourced read model changesets
- [Projections](../projections/index.md) — How read models are produced from events
- [Reducers](../reducers/index.md) — Imperative state-building from events
- [MongoDB Sink](../sinks/index.md#mongodb-sink) — How read model instances are stored in MongoDB
- [SQL Sink](../sinks/index.md#sql-sink) — How read model instances are stored in a SQL database
