# Subscribing to Events from External Event Stores

When you publish event types as part of a NuGet package — so other event stores can observe them via the inbox — you should annotate those event types with the `[EventStore]` attribute. This tells Chronicle which event store the events originate from, and allows observers to subscribe to the correct inbox sequence automatically.

## The `[EventStore]` Attribute

The attribute can be applied at two levels:

- **Class level** — directly on an individual event type
- **Assembly level** — once for all event types in a project

Both approaches identify the same information: the name of the source event store. When both are present, the class-level attribute takes precedence over the assembly-level one.

### Class-level attribute

Apply `[EventStore]` directly to any event type that originates from a foreign event store:

```csharp
[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);
```

The string argument is the **name of the source event store** — the same name used when setting up the subscription:

```csharp
await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",   // ← must match the EventStore attribute value
    builder => builder.WithEventType<ShipmentDispatched>());
```

### Assembly-level attribute

When all event types in a project belong to the same source event store, you can apply the `[EventStore]` attribute once at the assembly level instead of repeating it on every event type class.

**Preferred: configure via `.csproj`**

Add the attribute through MSBuild so no C# source file is needed:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
      <_Parameter1>fulfillment-service</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

**Alternative: C# file with assembly attribute**

You can also declare the attribute in a C# file (for example `AssemblyInfo.cs`):

```csharp
using Cratis.Chronicle.Events;

[assembly: EventStore("fulfillment-service")]
```

With either approach, every event type in that assembly is automatically treated as belonging to `fulfillment-service` without any per-class annotation.

## Publishing Event Types in a NuGet Package

A common pattern is to publish your public event contracts as a NuGet package. Consumers add a subscription and reference your package. The `[EventStore]` attribute on each event record provides all the routing information needed without any additional configuration in the consuming project.

### Using class-level attributes

Each event type is annotated individually:

```csharp
// FulfillmentService.Events/ShipmentDispatched.cs
using Cratis.Chronicle.Events;

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentFailed(Guid OrderId, string Reason);
```

### Using assembly-level attributes

When all event types belong to the same event store, set the attribute once in the project file — no C# boilerplate needed:

```xml
<!-- FulfillmentService.Events/FulfillmentService.Events.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
      <_Parameter1>fulfillment-service</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

Event types then carry no `[EventStore]` annotation at all — cleaner and less repetitive:

```csharp
// FulfillmentService.Events/ShipmentDispatched.cs
using Cratis.Chronicle.Events;

[EventType]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
public record ShipmentFailed(Guid OrderId, string Reason);
```

## Automatic Inbox Routing

When every event type handled by an observer is annotated with the same `[EventStore]` — either directly or via the assembly attribute — Chronicle automatically routes that observer to the corresponding inbox sequence — `inbox-{eventStoreName}`. You do not need to specify the event sequence explicitly.

### Reactors

```csharp
// No [Reactor(eventSequence: "inbox-fulfillment-service")] needed
public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
        => HandleDispatchedAsync(@event.OrderId, @event.TrackingNumber);

    public Task ShipmentFailed(ShipmentFailed @event, EventContext context)
        => HandleFailureAsync(@event.OrderId, @event.Reason);
}
```

### Reducers

```csharp
public class FulfillmentStatusReducer : IReducerFor<FulfillmentStatus>
{
    public FulfillmentStatus Reduce(ShipmentDispatched @event, FulfillmentStatus? current)
        => (current ?? new FulfillmentStatus()) with { Status = "Dispatched", TrackingNumber = @event.TrackingNumber };
}
```

### Projections

```csharp
public class FulfillmentProjection : IProjectionFor<FulfillmentReadModel>
{
    public void Define(IProjectionBuilderFor<FulfillmentReadModel> builder) =>
        builder
            .From<ShipmentDispatched>(_ => _
                .Set(m => m.TrackingNumber).To(e => e.TrackingNumber));
}
```

## Automatic Event Store Subscriptions

When a Reactor, Reducer, or Projection (declarative or model-bound) references event types that carry a `[EventStore]` attribute — either on the type itself or at the assembly level — Chronicle **automatically registers an event store subscription** for the referenced source event store during startup. You do not need to call `eventStore.Subscriptions.Subscribe(...)` manually.

The auto-registered subscription:

- Uses `auto-{sourceEventStoreName}` as the subscription identifier.
- Includes all the event type identifiers referenced by observers from that store.
- Is idempotent — calling `Subscribe` with the same ID is safe and produces no duplicate.

### Example — no manual subscription needed

Given the fulfillment events assembly published with the assembly-level `[EventStore]`:

```xml
<!-- FulfillmentService.Events/FulfillmentService.Events.csproj -->
<ItemGroup>
  <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
    <_Parameter1>fulfillment-service</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

And a reactor in your service:

```csharp
public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
        => HandleDispatchedAsync(@event.OrderId, @event.TrackingNumber);
}
```

Chronicle detects that `ShipmentDispatched` belongs to `fulfillment-service` and registers a subscription for that event store automatically during `RegisterAll()`. No startup boilerplate is required.

### Overriding the auto-subscription

If you need different filtering or a different subscription identifier than the default `auto-{storeName}`, register the subscription manually before `RegisterAll()` runs and remove the automatic one:

```csharp
await eventStore.Subscriptions.Subscribe(
    "my-custom-subscription",
    "fulfillment-service",
    builder => builder
        .WithEventType<ShipmentDispatched>()
        .WithEventType<ShipmentFailed>());
```

Manual and auto-registrations coexist — the auto-registration simply adds an additional subscription using its own identifier.

## Mixing Event Stores Is Not Allowed

An observer may only handle event types from a **single** event store. Mixing types annotated with different `[EventStore]` values on the same observer throws `MultipleEventStoresDefined` at startup:

```csharp
// ❌ This will throw MultipleEventStoresDefined
public class InvalidReactor : IReactor
{
    // ShipmentDispatched has [EventStore("fulfillment-service")]
    public Task Handle(ShipmentDispatched @event) => Task.CompletedTask;

    // OrderPlaced has [EventStore("ordering-service")]
    public Task Handle(OrderPlaced @event) => Task.CompletedTask;
}
```

Create a separate observer for each source event store.

## Schema Registration Metadata

When event types are registered with the Kernel, the source event store name is included in the registration metadata. This allows the Kernel to associate incoming inbox events with their originating store and display that information in the Workbench.

## See Also

- [Event Store Subscriptions](event-store-subscriptions.md) — setting up subscriptions between event stores
- [Outbox and Inbox](outbox-inbox.md) — how events flow between stores
