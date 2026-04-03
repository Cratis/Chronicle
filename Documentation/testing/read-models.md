---
uid: Chronicle.Testing.ReadModels
---
# Testing Read Models

`ReadModelScenario<TReadModel>` is a lightweight, in-process test utility that lets you verify the output of read model projections and reducers without a running Chronicle server or database.

It auto-detects how `TReadModel` is backed — by a reducer, a fluent projection, or a model-bound projection — and routes events through the appropriate engine.

## Why use ReadModelScenario

Integration tests against a live database are accurate but slow and fragile. `ReadModelScenario<TReadModel>` runs the same projection and reducer logic entirely in-process using null-stub storage, so your test suite stays fast without sacrificing correctness.

## Installation

`ReadModelScenario<TReadModel>` is in the `Cratis.Chronicle.Testing` NuGet package:

```bash
dotnet add package Cratis.Chronicle.Testing
```

## Basic usage

```csharp
var scenario = new ReadModelScenario<MyReadModel>();
await scenario.Given([new SomeEvent("value"), new SomeOtherEvent(42)]);

scenario.Instance.SomeProperty.ShouldBe("expected value");
```

`Given` feeds the supplied events through the read model's projection or reducer in order and populates `Instance` with the resulting state. Call `await scenario.Given([...])` before asserting.

## Optional initial state

Pass an initial state to the constructor when the read model starts from a non-default baseline:

```csharp
var initial = new MyReadModel { Count = 10 };
var scenario = new ReadModelScenario<MyReadModel>(initial);
await scenario.Given([new ItemAdded()]);

scenario.Instance.Count.ShouldBe(11);
```

## Auto-detection

`ReadModelScenario<TReadModel>` searches the current application's loaded assemblies for a handler in this order:

1. **Reducer** — a class implementing `IReducerFor<TReadModel>`.
2. **Fluent projection** — a class implementing `IProjectionFor<TReadModel>`.
3. **Model-bound projection** — `TReadModel` itself carries `[FromEvent<T>]` or `[Key]` attributes.

If none are found, `NoReadModelHandlerFound` is thrown.

## Example: reducer

```csharp
public record OrderSummary(string OrderId, decimal Total);

public class OrderSummaryReducer : IReducerFor<OrderSummary>
{
    public OrderSummary OnOrderCreated(OrderCreated @event, OrderSummary? current, EventContext context) =>
        new(@event.OrderId, 0m);

    public OrderSummary OnItemAdded(ItemAdded @event, OrderSummary current, EventContext context) =>
        current with { Total = current.Total + @event.Price };
}

// In your spec:
var scenario = new ReadModelScenario<OrderSummary>();
await scenario.Given([
    new OrderCreated("order-1"),
    new ItemAdded(9.99m),
    new ItemAdded(4.50m)
]);

scenario.Instance!.Total.ShouldBe(14.49m);
```

## Example: fluent projection

```csharp
[ReadModel]
public record ProductView(string Name, int Stock);

public class ProductViewProjection : IProjectionFor<ProductView>
{
    public void Define(IProjectionBuilderFor<ProductView> builder) =>
        builder
            .From<ProductCreated>(_ => _
                .Set(m => m.Name).To(e => e.Name))
            .From<StockAdjusted>(_ => _
                .Set(m => m.Stock).To(e => e.NewStock));
}

// In your spec:
var scenario = new ReadModelScenario<ProductView>();
await scenario.Given([
    new ProductCreated("Widget"),
    new StockAdjusted(100)
]);

scenario.Instance!.Name.ShouldBe("Widget");
scenario.Instance!.Stock.ShouldBe(100);
```

## Example: model-bound projection

```csharp
[ReadModel]
public record DeliveryStatus(
    [Key] string ShipmentId,
    [FromEvent<ShipmentDispatched>] string Carrier,
    [FromEvent<ShipmentDelivered>] DateTimeOffset? DeliveredAt);

// In your spec:
var scenario = new ReadModelScenario<DeliveryStatus>();
await scenario.Given([
    new ShipmentDispatched("FedEx"),
    new ShipmentDelivered(DateTimeOffset.UtcNow)
]);

scenario.Instance!.Carrier.ShouldBe("FedEx");
scenario.Instance!.DeliveredAt.ShouldNotBeNull();
```

## Notes

- `Instance` is `null` until `Given` is called, or if no events were processed.
- All events are applied to a single event source. Use separate scenarios to test multiple event sources.
- The `.ShouldBe()` assertions in the examples come from your test framework (e.g., [Cratis.Specifications](https://github.com/Cratis/Specifications), Shouldly, or FluentAssertions).
