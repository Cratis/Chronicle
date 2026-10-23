<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/pinned-generation-delivery.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Pin event generations before connecting

## Contents

- Standalone client
- Host integration
- Scope and registration
- Event-store subscription pins
- Product sources

Requires Chronicle **19.41.0+** for client reactors/reducers and **19.48.0+** for
typed materialized projections. Upgrade every kernel silo before enabling the
option. `Compatibility` remains the default; adding a generation alone does not
opt consumers in.

## Standalone client

This configuration excerpt uses `Cratis.Chronicle` and a running
local development kernel. Keep the client alive for observation; supply logging
and a service provider when the application's observers need them (see the main
skill's standalone checklist).

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation;

var options = new ChronicleOptions(ChronicleConnectionString.Development)
{
    EventGenerationDelivery = EventGenerationDelivery.Pinned
};
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore("People");
```

## Host integration

`AddCratisChronicle` extends `IHostApplicationBuilder` (an `IHostBuilder`
overload also exists), **not** `IServiceCollection`. Its `ChronicleClientOptions` inherits the delivery option.
This worker-host configuration excerpt uses `Cratis.Chronicle` and
`Microsoft.Extensions.Hosting`; add the application's hosted worker and start
the host as usual.

```csharp
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.AddCratisChronicle(options =>
{
    options.EventStore = "People";
    options.EventGenerationDelivery = EventGenerationDelivery.Pinned;
});
```

## Scope and registration

- Reactors and reducers pin the generation of the handler's CLR event type.
  Typed materialized projections (fluent, model-bound, or explicitly registered)
  pin each clause's declared CLR event generation. Live, catch-up, and replay
  (and, for reactors and reducers, partition retry) use kernel generation
  selection and schema-guided release.
- Projection pins cover `From`, `Join`, children, nested objects, removals,
  event-content keys, and parent keys. Stored join/parent-event lookups select
  and release through the same boundary as incoming events.
- **One generation per event identifier per observer/projection**, across every
  handler or clause, including children and nested objects. Different
  identifiers may pin different generations. Client validation throws
  `Cratis.Chronicle.Observation.MultipleGenerationsOfEventTypeInObserver` for
  conflicting pins; the kernel rejects them with
  `ObserverPinsMultipleGenerationsOfEventType`. Kernel observer subscription
  rejects an unregistered generation with
  `EventTypeGenerationNotRegisteredForObserver`; projection schema resolution
  can reject an unavailable pin with `PinnedEventTypeGenerationUnavailable`.
- Immediate/passive and wildcard (`all`/`FromAll`) projections stay
  `Compatibility` when built through the .NET client. A definition explicitly
  submitted as both wildcard and `Pinned` is unsupported and rejected by the
  kernel with `EventGenerationDeliveryNotSupported`; the option does not make
  wildcard pinning work.
- Ad-hoc queries/event reads, webhooks and captures are not opted in. Event-store
  subscriptions have their own builder opt-in below; the client-wide setting
  does not enable it.

For delivery-time migration, persisted PDL pins, and testing limits, read
[the migration reference](../../cratis-chronicle-event-type-migration/references/pinned-generations.md).

## Event-store subscription pins

Requires Chronicle 19.66.0 on client and kernel. Check `dotnet list package`
and the deployed kernel tag; older builders may throw
`EventStoreSubscriptionGenerationDeliveryNotSupported`. Compatibility remains
the default.

On `eventStore.Subscriptions.Subscribe(id, sourceStore, configure)`, configure
`WithGenerationDelivery(EventGenerationDelivery.Pinned)` and either
`WithEventType<TEvent>()` for that CLR type's declared generation or
`WithEventType(eventTypeId, generation)` for an explicit pin. This selects the
generation forwarded into the target inbox during live delivery, catch-up,
replay and partition retries; it is separate from the inbox observer's own pin.

Both stores must register the generation. Registration refuses a missing
source/target generation or two generations of the same identifier. If the
target loses its pin at forwarding time, the failing batch appends nothing and
does not advance past the failing event; earlier acknowledged events stay
acknowledged. Changing only a pin or delivery mode rebinds the subscription in
place, without an unsubscribe. Verify migration and protected-value release
through real-client integration, not just a handler fed the desired CLR type.

## Product sources

Source: `Cratis/Chronicle`:
`Source/Clients/DotNET/ChronicleOptions.cs`,
`Observation/EventGenerationDelivery.cs`,
`ChronicleHostApplicationBuilderExtensions.cs`,
`Projections/ProjectionGenerationDeliveryValidation.cs`, and
`Source/Kernel/Core/Projections/Engine/ProjectionFactory.cs`.
The option and reactor/reducer validation are also in the client.

See [Chronicle's pinned observer delivery documentation](https://github.com/Cratis/Chronicle/blob/main/Documentation/concepts/event-type-migrations.mdx#opt-in-to-pinned-observer-delivery).

| Copying a version out of the repository | It is a placeholder; take it from nuget.org |