# Projections — Reference

## Model-bound projections (preferred)

Put projection metadata on the read model first. This is the default choice for Cratis projects because it keeps the read model and its projection behavior together.

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record InvoiceInfo(
    [Key] Guid Id,
    [FromEvent<InvoiceCreated>] string Number,
    [AddFrom<LineItemAdded>(nameof(LineItemAdded.Price))] decimal RunningTotal,
    [SetFromContext<InvoicePaid>(nameof(EventContext.Occurred))] DateTimeOffset? PaidAt);
```

For child relationships where later child events arrive on the child event source, set `parentKey` on the child type's class-level `FromEvent`:

```csharp
public record Invoice(
    [Key] Guid Id,
    [ChildrenFrom<LineItemAdded>(
        key: nameof(LineItemAdded.LineItemId),
        identifiedBy: nameof(LineItem.Id),
        parentKey: nameof(LineItemAdded.InvoiceId))]
    IEnumerable<LineItem> Lines);

[FromEvent<LineItemRenamed>(parentKey: nameof(LineItemRenamed.InvoiceId))]
public record LineItem(
    [Key] Guid Id,
    string Description);
```

### Attribute reference

| Attribute | Equivalent builder |
| --------- | ------------------ |
| `[Key]` | Default key (event source ID) |
| `[FromEvent<T>]` | `.AutoMap()` for event T |
| `[FromEvent<T>(key: nameof(T.Prop))]` | `.UsingKey(e => e.Prop)` |
| `[FromEvent<T>(parentKey: nameof(T.Prop))]` | `.UsingParentKey(e => e.Prop)` on child projections |
| `[SetFrom<T>(nameof(...))]` | `.Set(...).To(...)` |
| `[AddFrom<T>(nameof(...))]` | `.Add(...).With(...)` |
| `[SubtractFrom<T>(nameof(...))]` | `.Subtract(...).With(...)` |
| `[SetFromContext<T>(nameof(...))]` | `.Set(...).ToEventContextProperty(...)` |
| `[Increment<T>]` | counter increment |
| `[Decrement<T>]` | counter decrement |
| `[RemovedWith<T>]` | `.RemovedWith<T>()` |
| `[Passive]` | `.Passive()` |
| `[NotRewindable]` | `.NotRewindable()` |

---

## Declarative projection builder (`IProjectionFor<T>`) — fallback

```csharp
public class InvoiceProjection : IProjectionFor<Invoice>
{
    public void Define(IProjectionBuilderFor<Invoice> builder) => builder
        .From<InvoiceCreated>(from => from.AutoMap())        // map all matching property names
        .From<InvoicePaid>(from => from
            .Set(m => m.PaidAt).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.Status).WithValue(InvoiceStatus.Paid))
        .From<LineItemAdded>(from => from
            .Add(m => m.TotalAmount).With(e => e.Price))
        .Join<CustomerRegistered>(j => j
            .On(m => m.CustomerId)
            .Set(m => m.CustomerName).To(e => e.Name))
        .RemovedWith<InvoiceDeleted>()
        .Children<LineItem>(m => m.Lines, cb => cb
            .IdentifiedBy(li => li.LineItemId)
            .From<LineItemAdded>(from => from.AutoMap())
            .RemovedWith<LineItemRemoved>());
}
```

### Builder method reference

| Method | Purpose |
| ------ | ------- |
| `.From<TEvent>(cb)` | Handle an event type |
| `.AutoMap()` | Map all event properties with matching names to the read model |
| `.Set(m => m.Prop).To(e => e.Prop)` | Explicit property mapping |
| `.Set(m => m.Prop).WithValue(val)` | Set a constant |
| `.Set(m => m.Prop).ToEventContextProperty(c => c.X)` | Map from event metadata |
| `.Add(m => m.Prop).With(e => e.X)` | Add (numeric) |
| `.Subtract(m => m.Prop).With(e => e.X)` | Subtract |
| `.Count(m => m.Prop)` | Increment a counter |
| `.Join<TEvent>(j => j.On(key).Set(...))` | Cross-stream join another event |
| `.RemovedWith<TEvent>()` | Delete the read model on this event |
| `.Children<TChild>(m => m.Coll, cb)` | Manage a child collection |
| `.FromEvery(cb)` | Apply mapping to every event type |
| `.UsingKey(e => e.Prop)` | Override the key (default: event source ID) |
| `.Passive()` | On-demand only, no active observer |
| `.NotRewindable()` | Forward-only, no replay |

### Event context properties

```csharp
.ToEventContextProperty(c => c.Occurred)       // DateTimeOffset
.ToEventContextProperty(c => c.EventSourceId)  // string
.ToEventContextProperty(c => c.SequenceNumber) // long
.ToEventContextProperty(c => c.CorrelationId)  // Guid
```

### Composite keys

```csharp
builder.UsingCompositeKey<MonthlyReport>(key => key
    .Set(k => k.Year).ToEventContextProperty(c => c.Occurred.Year)
    .Set(k => k.Month).ToEventContextProperty(c => c.Occurred.Month));
```

## Reading projected read models

```csharp
// Inject IReadModels in a controller
[HttpGet("{id}")]
public async Task<AccountSummary?> Get(Guid id)
    => await readModels.GetOne<AccountSummary>(id);

// Strong consistency (replay events synchronously before returning)
var account = await readModels.GetOneWithImmediateProjection<AccountSummary>(id);

// All instances
var all = await readModels.GetAll<AccountSummary>();
```

---

## Appended event metadata and projections

When you append events, you can set tags, event source type, and event stream type:

```csharp
await eventLog.Append(
    EventSourceId.New(),
    new OrderPlaced(42m),
    eventStreamType: "fulfillment",
    eventSourceType: "order",
    tags: ["priority"]);
```

Projection definitions do not use `[FilterEventsByTag]`, `[EventSourceType]`, or `[EventStreamType]` as observer filters. Projections choose input through event types, joins, and event sequence selection.

Use appended metadata when you need:

- A reducer or reactor alongside the projection to observe only matching events
- Event context values inside projection mappings
- Consistent metadata across downstream observers that react to the same append operation

`[Tag]` and `[Tags]` on a projection label the projection definition; they do not filter appended events.

For reducer and reactor filtering examples, see `Documentation/events/filtering/`.
