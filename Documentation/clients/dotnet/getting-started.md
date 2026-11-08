---
title: Cratis Chronicle .NET client connection and first append
description: Connect a .NET application to Cratis Chronicle, define an event, and append it to an event log, or use host integration for a service.
---

Use the Cratis Chronicle .NET client to record a fact from your C# application in an event log. This guide connects a client to a local kernel, opens an event store, and appends a `BookAdded` event. You can then add projections to build read models or reactors to respond to that event.

## Connect

Have the Chronicle kernel running locally and add the `Cratis.Chronicle` package to your application; the [client setup page](./index.md#install) covers package selection. Then connect with the development connection string:

```csharp
using Cratis.Chronicle;

using var client = new ChronicleClient(ChronicleConnectionString.Development);
var eventStore = await client.GetEventStore("Quickstart");
```

`Quickstart` names the event store this client will use. `ChronicleConnectionString.Development` connects to the default local kernel with development credentials.

`ChronicleConnectionString.Default` also points at `localhost:35000` but names no credentials, so it falls back to the same development credentials. Use `auth=none` for an anonymous connection. For another environment, pass an explicit `new ChronicleConnectionString("chronicle://...")` with its host and credentials, and set `skipTlsValidation=false` when its certificate is verifiable. See [Client TLS](/chronicle/configuration/tls/).

## Define an event

An event records something that has happened. Mark the record with `[EventType]` so the client can discover it and register its schema with Chronicle:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);
```

## Append the event

Use `bookId` to identify the book whose history this event belongs to. That event source id stays outside the payload: Chronicle stores it in the event context, while the `BookAdded` record carries the title and ISBN.

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

`result.IsSuccess` tells you whether the append succeeded. The stored event can then be processed by projections, reducers, reactors, and subscriptions observing that event sequence. Append success does not by itself mean those observers have finished processing it; see [shared append semantics](/chronicle/events/appending/) before reading a model that depends on the new event.

## Use a host integration

For ASP.NET Core and Worker Service applications, prefer the host integration instead of creating the client manually. `AddCratisChronicle` extends the application builder (`WebApplicationBuilder` or `IHostApplicationBuilder`), not `builder.Services`:

```csharp
builder.AddCratisChronicle(options => options.EventStore = "Quickstart");
```

The host integration makes the event store, event log, read models, projections, reducers, reactors, constraints, and related services available through dependency injection. Follow the hosting guide for your application type below to complete startup. For an ASP.NET Core service, [expose a readiness health check](./health-checks.md) so traffic waits for the connection and artifact registration.

## Pin reactor and reducer generations

If independently deployed consumers handle different generations of the same event type, use pinned
delivery to have the kernel select and deliver the generation each reactor or reducer handles.
Configure it before connecting:

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
