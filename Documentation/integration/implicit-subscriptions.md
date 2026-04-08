---
uid: implicit-subscriptions
---

# Implicit Event Store Subscriptions

Chronicle supports **implicit subscriptions** — automatic routing of events from a source event store's outbox to the correct inbox sequence, driven entirely by `[EventStore]` attributes on event types.

Implicit subscriptions are ideal when:
- Event types are published as a NuGet package by the source service
- You want zero configuration — just reference the package and observe the events
- The consuming service doesn't need fine-grained control over which event types to forward

## How Implicit Subscriptions Work

When every event type handled by an observer shares the same `[EventStore]` attribute, Chronicle infers the source event store name and automatically routes the observer to the corresponding inbox sequence. A **kernel-side subscription is created and persisted automatically** the first time an observer targets those events.

## The `[EventStore]` Attribute

Apply `[EventStore]` to any event type that originates from a foreign event store:

```csharp
[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentFailed(Guid OrderId, string Reason);
```

The string argument is the **name of the source event store** — the same name you would use if setting up an explicit subscription.

## Publishing Event Types in a NuGet Package

A common pattern is to publish your public event contracts as a NuGet package. Consumers add a reference to your package; the `[EventStore]` attribute provides all the routing information needed without additional configuration.

A typical public events package looks like this:

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

## Assembly-Level Configuration with `[EventStore]`

To avoid repeating `[EventStore]` on every event type, apply the attribute once at the assembly level in your **GlobalUsings.cs** or **AssemblyInfo.cs** file. This is the **recommended approach** for creating reusable event contracts.

### The `.Contracts` Project Pattern

Create a separate, thin project with the naming convention `{ServiceName}.Events.Contracts` to publish your event types:

```text
FulfillmentService.Events.Contracts/
├── AssemblyInfo.cs
├── ShipmentDispatched.cs
├── ShipmentFailed.cs
└── FulfillmentService.Events.Contracts.csproj
```

This `.Contracts` project contains **only your event type definitions** and minimal dependencies. It's lightweight, fast to build, and safe to share as a NuGet package.

### Project File Configuration

Configure your `.Contracts` .csproj with appropriate metadata for sharing:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>FulfillmentService.Events.Contracts</AssemblyName>
    <PackageId>FulfillmentService.Events.Contracts</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Title>Fulfillment Service Event Contracts</Title>
    <Description>Shared event types published by FulfillmentService</Description>
    <Authors>Your Team</Authors>
    <RepositoryUrl>https://github.com/yourorg/FulfillmentService</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cratis.Chronicle" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### Assembly-Level Attribute Definition

In your `.Contracts` project, create an **AssemblyInfo.cs** file or use **GlobalUsings.cs** to apply `[EventStore]` once:

### Option 1: Using AssemblyInfo.cs

```csharp
// FulfillmentService.Events.Contracts/AssemblyInfo.cs
using Cratis.Chronicle.Events;

[assembly: EventStore("fulfillment-service")]
```

### Option 2: Using GlobalUsings.cs (Recommended)

```csharp
// FulfillmentService.Events.Contracts/GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using Cratis.Chronicle.Events;

[assembly: EventStore("fulfillment-service")]
```

### Option 3: Using `<AssemblyAttribute>` in the .csproj (No C# File Needed)

You can apply the assembly-level `[EventStore]` attribute directly from your `.csproj`, without creating any C# file. Add an `<AssemblyAttribute>` item to your project:

```xml
<ItemGroup>
  <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
    <_Parameter1>fulfillment-service</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

MSBuild will emit `[assembly: Cratis.Chronicle.Events.EventStoreAttribute("fulfillment-service")]` in the auto-generated `AssemblyInfo.cs` file — no manual C# file required.

> **Note:** This approach requires `<GenerateAssemblyInfo>` to be `true` (the default). If your project sets `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`, the `<AssemblyAttribute>` item will have no effect since it relies on the auto-generated file.

### Event Type Definitions

Now your event types only need `[EventType]`:

```csharp
// FulfillmentService.Events.Contracts/ShipmentDispatched.cs
namespace FulfillmentService.Events;

[EventType]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
public record ShipmentFailed(Guid OrderId, string Reason);
```

All events in this assembly are automatically associated with the `fulfillment-service` event store.

### Publishing and Consuming

**In your FulfillmentService (source):**
- Reference your own `.Contracts` project
- Event types are published to `fulfillment-service` outbox

**In consuming services:**
- Add a NuGet reference to `FulfillmentService.Events.Contracts`
- Create observers (reactors, projections, reducers) that handle the event types
- Events are automatically routed to the `inbox-fulfillment-service` sequence
- **No explicit subscription or configuration needed**

Example consumer setup:

```csharp
// OrderService/Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddChronicle(c => c
    .UseMongoDB("mongodb://localhost:27017/order-service")
    .UseEventStore("order-service"));

// Just reference the events from the NuGet package
using var app = builder.Build();
// Reactors/projections that observe FulfillmentService.Events types
// are automatically routed to the inbox
```

## Automatic Inbox Routing

When an observer (reactor, projection, or reducer) handles event types all annotated with the same `[EventStore]` name, Chronicle automatically routes that observer to the corresponding inbox sequence — `inbox-{eventStoreName}`. No explicit subscription or event sequence configuration is needed.

### Reactors

```csharp
// No explicit event sequence needed
public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
        => HandleDispatchedAsync(@event.OrderId, @event.TrackingNumber);

    public Task ShipmentFailed(ShipmentFailed @event, EventContext context)
        => HandleFailureAsync(@event.OrderId, @event.Reason);

    Task HandleDispatchedAsync(Guid id, string tracking) => Task.CompletedTask;
    Task HandleFailureAsync(Guid id, string reason) => Task.CompletedTask;
}
```

### Reducers

```csharp
public class FulfillmentStatusReducer : IReducerFor<FulfillmentStatus>
{
    public FulfillmentStatus Reduce(ShipmentDispatched @event, FulfillmentStatus? current)
        => (current ?? new FulfillmentStatus()) with 
        { 
            Status = "Dispatched", 
            TrackingNumber = @event.TrackingNumber 
        };

    public FulfillmentStatus Reduce(ShipmentFailed @event, FulfillmentStatus? current)
        => (current ?? new FulfillmentStatus()) with { Status = "Failed" };
}
```

### Projections

```csharp
public class FulfillmentProjection : IProjectionFor<FulfillmentReadModel>
{
    public void Define(IProjectionBuilderFor<FulfillmentReadModel> builder) =>
        builder
            .From<ShipmentDispatched>(_ => _
                .Set(m => m.TrackingNumber).To(e => e.TrackingNumber)
                .Set(m => m.Status).To(e => "Dispatched"))
            .From<ShipmentFailed>(_ => _
                .Set(m => m.Status).To(e => "Failed"));
}
```

## Subscription Lifecycle

The kernel automatically manages the implicit subscription:

1. **Creation** — When the first observer targeting the event type is registered, the kernel creates a persistent subscription to the source outbox.
2. **Persistence** — The subscription is stored and persists across kernel restarts.
3. **Forwarding** — Events appended to the source outbox are continuously forwarded to the inbox.
4. **Cleanup** — If no observers are targeting events from a source, the subscription remains active but idle. Subscriptions are not automatically removed.

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

To observe events from multiple sources, create a separate observer for each source event store:

```csharp
public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
        => HandleFulfillmentAsync(@event);
    
    Task HandleFulfillmentAsync(ShipmentDispatched @event) => Task.CompletedTask;
}

public class OrderingReactor : IReactor
{
    public Task OrderPlaced(OrderPlaced @event, EventContext context)
        => HandleOrderAsync(@event);
    
    Task HandleOrderAsync(OrderPlaced @event) => Task.CompletedTask;
}
```

## Schema Registration and Metadata

When event types are registered with the kernel, the source event store name is included in the registration metadata. This allows the kernel to associate inbox events with their originating store and display that information in the Workbench.

## When to Use Implicit Subscriptions

**Use implicit subscriptions when:**
- Event types are published in a shared NuGet package
- All events from a source should be forwarded (or at least all events you observe on)
- You want zero configuration overhead
- The package owner controls the event types and their `[EventStore]` attributes

**Use [explicit subscriptions](explicit-subscriptions.md) when:**
- You need fine-grained control over which event types are forwarded
- You want to manually manage subscription lifecycle and cleanup
- Events are not in a shared NuGet package, or you need dynamic subscription configuration
- You need to subscribe to only a subset of events from a source

## See Also

- [Explicit Event Store Subscriptions](explicit-subscriptions.md) — manual subscription using the Subscribe API
- [Outbox and Inbox](outbox-inbox.md) — conceptual explanation of how events flow between stores
- [Reactors](../reactors/index.md) — processing inbox events with reactors
- [Projections](../projections/index.md) — building read models from inbox events
