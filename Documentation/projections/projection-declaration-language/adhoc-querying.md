# Ad-hoc Querying with `IProjections.Query()`

The `IProjections.Query()` method lets you run a projection ad-hoc from the .NET client — without defining a read model type or registering a permanent projection. You write a PDL declaration, send it to the server, and get back the projected read model entries as JSON strings.

> [!WARNING]
> **This API is not a replacement for registered projections.**
> Every call to `Query()` replays events from the beginning of the event sequence up to an internal limit.
> It is designed for **small, bounded event volumes** — tooling, back-office dashboards, diagnostics,
> and similar scenarios where you have control over (or can reason about) the number of relevant events.
> Using it in high-throughput code paths or against event sequences with tens of thousands of events
> will be slow and resource-intensive.

## Basic Usage

Inject `IProjections` into your service, then call `Query()` with a PDL declaration string:

```csharp
using Cratis.Chronicle.Projections;
using System.Text.Json;

public class OrderQueryService(IProjections projections)
{
    public async Task<IEnumerable<OrderSummary>> GetOrderSummaries()
    {
        var result = await projections.Query("""
            projection OrderSummary
              from OrderPlaced
            """);

        return result.ReadModelEntries
            .Select(json => JsonSerializer.Deserialize<OrderSummary>(json)!);
    }
}
```

`ReadModelEntries` is a `IReadOnlyList<string>` where each element is the JSON representation of one projected read model instance.

## Explicit vs. Inferred Read Model

You can optionally specify a `=> ReadModelType` in your declaration. When you do, the server resolves the schema from the registered read model definition. When you omit it, the schema is **inferred** from the event properties.

```csharp
// Inferred — schema derived from OrderPlaced and OrderShipped event properties
var result = await projections.Query("""
    projection Orders
      from OrderPlaced
      from OrderShipped
    """);

// Explicit — schema comes from the registered 'OrderReadModel' type
var result = await projections.Query("""
    projection Orders => OrderReadModel
      from OrderPlaced
      from OrderShipped
    """);
```

When using an inferred schema with multiple `from` blocks, all events that contribute the same property name must use compatible types. The compiler reports an error at query time if there is a mismatch:

```csharp
// This declaration will throw UnableToQueryProjection:
// OrderPlaced.value is string, but OrderShipped.value is int
var result = await projections.Query("""
    projection Bad
      from OrderPlaced   // value: string
      from OrderShipped  // value: int  → incompatible types
    """);
```

## Error Handling

If the declaration contains syntax or type errors, `Query()` throws `UnableToQueryProjection`:

```csharp
try
{
    var result = await projections.Query("""
        projection Orders
          from OrderPlaced
        """);
}
catch (UnableToQueryProjection ex)
{
    Console.WriteLine(ex.Message);
}
```

## Targeting a Different Event Sequence

By default `Query()` reads from the `event-log` sequence. Pass a different sequence identifier as the second argument:

```csharp
var result = await projections.Query(
    """
    projection InboxMessages
      from MessageReceived
    """,
    eventSequenceId: "inbox");
```

## Practical Use Cases

`Query()` is well-suited for situations where you want projection results without the overhead of defining and registering a permanent read model:

- **Back-office tooling** — maintenance screens for technical staff that need one-off views into the event log.
- **Diagnostic dashboards** — quick summaries during incident investigation.
- **Development utilities** — exploring what data an event sequence contains while building a feature.
- **Integration tests** — asserting projected state in test scenarios without registering a projection.

## Limitations

| Concern | Detail |
|---------|--------|
| **Event volume** | The server reads up to an internal maximum (currently 1 000 events). Sequences with more matching events will return incomplete results. |
| **No persistence** | Results are not stored. Every call replays the relevant portion of the event log. |
| **No registration** | A projection without `=> ReadModelType` can never be saved as a permanent projection. |
| **No change notifications** | Results are a point-in-time snapshot; there is no observable / reactive variant of this API. |
