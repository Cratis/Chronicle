---
uid: Chronicle.Testing.Reactors.Scenario
---
# ReactorScenario

`ReactorScenario<TReactor>` is a lightweight, in-process test utility that lets you verify reactor side-effects without a running Chronicle server, gRPC transport, or observer registration.

It activates a fresh instance of `TReactor` from the provided service provider and routes events directly through the `ReactorInvoker` — the same execution path used in production.

## Why use ReactorScenario

End-to-end tests that require a live Chronicle server are accurate but slow. `ReactorScenario<TReactor>` drives the same reactor logic in-process so your specs remain fast and isolated without losing coverage of the handler logic.

## Installation

`ReactorScenario<TReactor>` is in the `Cratis.Chronicle.Testing` NuGet package:

```bash
dotnet add package Cratis.Chronicle.Testing
```

## Basic usage

```csharp
var scenario = new ReactorScenario<MyReactor>(serviceProvider);
await scenario.Given
    .ForEventSource(someId)
    .Events(new SomeEvent("value"), new SomeOtherEvent(42));

// Assert on side-effects captured by mocks in serviceProvider
myMock.Received(1).DoSomething("value");
```

`Given` is a fluent builder: call `ForEventSource(id)` to specify the event source, then `Events(...)` to route events through the reactor in order.

## Injecting dependencies

Pass an `IServiceProvider` to the constructor to supply mocks and stubs that the reactor depends on:

```csharp
var orderRepository = Substitute.For<IOrderRepository>();
var services = new ServiceCollection()
    .AddSingleton(orderRepository)
    .BuildServiceProvider();

var scenario = new ReactorScenario<OrderNotificationReactor>(services);
await scenario.Given
    .ForEventSource("order-123")
    .Events(new OrderShipped("order-123", "FedEx"));

await orderRepository.Received(1).MarkAsShipped("order-123");
```

When no `IServiceProvider` is provided, `ReactorScenario<TReactor>` uses a `DefaultServiceProvider` that constructs the reactor via its default constructor.

## Example: email notification

```csharp
[EventType("order-shipped")]
public record OrderShipped(string OrderId, string Carrier);

public class OrderNotificationReactor(IEmailService emailService) : IReactor
{
    public async Task OnOrderShipped(OrderShipped @event, EventContext context)
    {
        await emailService.SendShippingConfirmation(@event.OrderId, @event.Carrier);
    }
}

// In your spec:
var emailService = Substitute.For<IEmailService>();
var services = new ServiceCollection()
    .AddSingleton(emailService)
    .BuildServiceProvider();

var scenario = new ReactorScenario<OrderNotificationReactor>(services);
await scenario.Given
    .ForEventSource("order-123")
    .Events(new OrderShipped("order-123", "DHL"));

await emailService.Received(1).SendShippingConfirmation("order-123", "DHL");
```

## Example: multiple events across event sources

```csharp
var scenario = new ReactorScenario<TenantSyncReactor>(services);

// Events from two different tenants
await scenario.Given
    .ForEventSource("tenant-A")
    .Events(new TenantActivated("tenant-A"));

await scenario.Given
    .ForEventSource("tenant-B")
    .Events(new TenantActivated("tenant-B"));

// Both activations should have been handled
await syncService.Received(2).SyncTenant(Arg.Any<string>());
```

## Notes

- A fresh instance of `TReactor` is activated from the service provider for each `Events(...)` call, matching the production behavior where a new scope is created per event batch.
- Event handling uses the same `ReactorInvoker` as in production, so convention-based method discovery (`On<TEvent>`) works identically.
- The `.Received()` assertions in the examples come from [NSubstitute](https://nsubstitute.github.io/). Any mocking framework works.
