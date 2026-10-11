---
title: Get started with the .NET client
description: Connect a .NET application to Chronicle and append an event.
---

Use the .NET client when you want direct typed access to Chronicle from a C# application. The smallest useful loop is:

1. create a `ChronicleClient`
2. open an event store
3. append a fact to an event log
4. let projections, reducers, and reactors observe that fact

## Connect

For local development, start the Chronicle kernel and connect with the development connection string.

```csharp
using Cratis.Chronicle;

using var client = new ChronicleClient(ChronicleConnectionString.Development);
var eventStore = await client.GetEventStore("Quickstart");
```

`ChronicleConnectionString.Development` points at the default local Chronicle kernel with development credentials. `ChronicleConnectionString.Default` also points at `localhost:35000`; it names no credentials, so connecting with it falls back to the same development credentials (`auth=none` is the anonymous option). To connect to any other environment, pass an explicit `new ChronicleConnectionString("chronicle://...")` carrying that environment's host and credentials, and set `skipTlsValidation=false` when its certificate is verifiable — see [Client TLS](/chronicle/configuration/tls/).

## Define an event

Events are immutable facts. In .NET, mark event records with `[EventType]` so Chronicle can discover and register their schema.

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);
```

## Append the event

Append the event against the event source it belongs to. The event source id is not part of the event payload; Chronicle stores it in the event context.

```csharp
var bookId = "book-123";

var result = await eventStore.EventLog.Append(
    bookId,
    new BookAdded("The Pragmatic Programmer", "978-0135957059"));

if (!result.IsSuccess)
{
    // Decide whether to retry or surface the append failure to the caller.
}
```

After the append succeeds, Chronicle persists the event and forwards it to the projections, reducers, reactors, and subscriptions that observe the event sequence.

## Use a host integration

For ASP.NET Core and Worker Service applications, prefer the host integration instead of creating the client manually. `AddCratisChronicle` extends the application builder (`WebApplicationBuilder` or `IHostApplicationBuilder`), not `builder.Services`:

```csharp
builder.AddCratisChronicle(options => options.EventStore = "Quickstart");
```

The host integration registers the event store, event log, read models, projections, reducers, reactors, constraints, and related services in dependency injection.

## Pin reactor and reducer generations

When independently deployed consumers use different generations of an event type, opt in to kernel
selection and release of the generation each reactor or reducer handles. Configure this before connecting:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Observation;

var options = new ChronicleOptions(ChronicleConnectionString.Development)
{
    EventGenerationDelivery = EventGenerationDelivery.Pinned
};
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore("Quickstart");
```

`Compatibility` remains the default. `Pinned` applies to client reactors and reducers on live delivery,
catch-up, replay, and partition retry; it does not change projections or event reads. Upgrade all kernel
silos before enabling it. Each observer must handle only one generation per event type, and that
generation must be registered. The delivered `EventContext.AppendedGeneration` is separate from the
handler's generation and is null for legacy events whose original generation is unknown.

See [pinned observer delivery](../../concepts/event-type-migrations.mdx#opt-in-to-pinned-observer-delivery)
for on-demand migration, revision, and hash behavior.

## Next

- [Console quickstart](/chronicle/get-started/console/)
- [ASP.NET Core hosting](/chronicle/get-started/aspnetcore/)
- [Worker Service hosting](/chronicle/get-started/worker/)
- [Shared append semantics](/chronicle/events/appending/)
