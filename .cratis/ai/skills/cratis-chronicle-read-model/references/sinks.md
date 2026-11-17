<!-- cratis-ai-managed: skills/cratis-chronicle-read-model/references/sinks.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Named sinks for read models

## Contents

- Default and named sinks
- Choose a sink with `[Sink]`
- Client defaults
- Passive read models have no sink
- Changing a sink replays
- Refusals

Requires Chronicle 19.144.0 for `[Sink]`, `DefaultSinkName`, `DefaultGraphSinkName`, CHR0069 and
the Kernel `sinks` configuration (check the `Cratis.Chronicle` package and the Kernel image tag;
both must be at least that version). Older clients have no `Cratis.Chronicle.Sinks.SinkAttribute`.

## Default and named sinks

A sink is where a read model is stored. Without any choice every read model goes to the Kernel's
default sink: the client's `DefaultSinkTypeId` (MongoDB unless changed) on the Kernel's `storage`
connection. A **named sink** is a sink type with a connection of its own, configured by name in
the Kernel's `sinks` section (`cratis-chronicle-hosting`). Use one when some read models belong in
another database or engine, such as reporting tables in PostgreSQL while the rest stays in MongoDB.
The client only names the sink; it never carries a connection string.

## Choose a sink with `[Sink]`

`Cratis.Chronicle.Sinks.SinkAttribute` goes on the **read model type**, whatever builds it: a
model-bound read model, one filled by an `IProjectionFor<T>`, or one folded by a reducer. Never put
it on the projection or reducer class; the builder writes wherever its read model is stored.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Sinks;

[EventType]
public record OrderPlaced(Guid CustomerId, decimal Amount);

[EventType]
public record ShipmentDispatched(Guid OrderId, string Carrier);

[Sink("reporting")]
[FromEvent<ShipmentDispatched>(key: nameof(ShipmentDispatched.OrderId))]
public record DispatchedShipment([property: Key] Guid OrderId, string Carrier);

[Sink("reporting")]
public record CustomerRevenue([property: Key] Guid CustomerId, decimal Total);

public class CustomerRevenueProjection : IProjectionFor<CustomerRevenue>
{
    public void Define(IProjectionBuilderFor<CustomerRevenue> builder) =>
        builder.From<OrderPlaced>(from => from
            .UsingKey(@event => @event.CustomerId)
            .Add(model => model.Total).With(@event => @event.Amount));
}

[Sink("reporting")]
public record OrderCount(int Orders);

public class OrderCountReducer : IReducerFor<OrderCount>
{
    public OrderCount OnOrderPlaced(OrderPlaced @event, OrderCount? current, EventContext context) =>
        new((current?.Orders ?? 0) + 1);
}
```

The name matches a `sinks.configurations` entry, compared without regard to case. The sink's
configured `type` decides the engine: a read model naming a `SQL` sink is stored in SQL even when
the client's `DefaultSinkTypeId` is MongoDB. Queries keep reading through Chronicle's read-model
APIs; a direct `IMongoCollection<T>` or SQL query must target the database the named sink points
at, not the Kernel's default storage.

Graphs choose a sink the same way: read
[graph projections](../../cratis-chronicle-projection/references/graph-projections.md#sinks).

## Client defaults

To send everything that names no sink to a named sink, set the client defaults. Both are empty
by default, which leaves the choice to the Kernel's `sinks.readModels` and `sinks.graphs`:

```csharp
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options =>
        {
            options.DefaultSinkName = "reporting";
            options.DefaultGraphSinkName = "people";
        });
    }
}
```

The order is: `[Sink]` on the type, then `ChronicleOptions.DefaultSinkName` (or
`DefaultGraphSinkName` for graphs), then the Kernel's `sinks.readModels` (or `sinks.graphs`), then
the Kernel's own storage.

## Passive read models have no sink

A `[Passive]` read model is never materialized, so it has nowhere to be stored. Combining
`[Passive]` with `[Sink]` is analyzer error `CHR0069`: remove `[Sink]` to keep it passive, or
remove `[Passive]` to store it in the named sink. Event targets (a projection or reducer publishing
to an event sequence) ignore sink names.

## Changing a sink replays

Moving a read model to another sink, by a different `[Sink]`, client default or Kernel default,
rebuilds it there: the Kernel replays the projections and reducers that fill it into the new sink,
as it does for a changed definition. The
`observers.definitionEvolution` policy applies, so a `Manual` policy records a replay
recommendation instead of replaying. A read model whose sink is unchanged is not replayed. Read
[definition changes](../../cratis-chronicle-projection/references/definition-changes.md) before
deploying a sink move on a large history.

## Refusals

| Failure | Cause | Fix |
| --- | --- | --- |
| `CHR0069` (analyzer, error) | `[Passive]` and `[Sink]` on one type | Remove one of them |
| `UnknownSinkName` at registration | The Kernel has no `sinks.configurations` entry with that name | Add the configuration, or fix the name |
| `SinkConfigurationIsForAnotherKindOfReadModel` | A read model names a `Neo4j` sink, or a graph a `MongoDB`/`SQL` sink | Name a sink of the right kind |
| `GraphHasConflictingSinks` | Nodes and edges of one model-bound graph name different sinks | Give every element of the graph the same `[Sink]` |
