---
name: write-specs-readmodels
description: Use this skill when asked to write tests or specs for read model projections, reducers, or model-bound projections using ReadModelScenario in a Cratis-based project. Produces infrastructure-free in-process specs using ReadModelScenario<TReadModel>.
---

# Writing Read Model Specs with ReadModelScenario

Use `ReadModelScenario<TReadModel>` to test read model projections and reducers entirely in-process — no Chronicle server, database, or network required.

## When to use this skill

- Testing that a reducer correctly builds state from a sequence of events
- Testing that a fluent `IProjectionFor<T>` projection maps event properties to read model properties
- Testing that a model-bound projection (`[FromEvent<T>]`, `[Key]`) maps correctly
- Testing boundary conditions: first event, multiple events, different event sources

For testing **event appending, constraints, or concurrency**, use `EventScenario` instead.

---

## Package

```bash
dotnet add package Cratis.Chronicle.Testing
```

---

## Basic structure

```csharp
// In your spec file (follows BDD Establish/Because/should_ pattern)
public class when_<event_sequence_description> : Specification
{
    ReadModelScenario<TReadModel> _scenario;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<TReadModel>();
        await _scenario.Given
            .ForEventSource(<eventSourceId>)
            .Events(new <EventType>(<args>));
    }

    [Fact] void should_<expected_state>() => _scenario.Instance!.<Property>.ShouldEqual(<expectedValue>);
}
```

---

## Step 1 — Create the scenario

Always create a **new** `ReadModelScenario<TReadModel>` per test. Optionally pass an initial state:

```csharp
// Default (empty) initial state
var scenario = new ReadModelScenario<MyReadModel>();

// With an initial state baseline
var scenario = new ReadModelScenario<MyReadModel>(new MyReadModel { Count = 10 });
```

---

## Step 2 — Seed events via the fluent Given builder

```csharp
await scenario.Given
    .ForEventSource(myId)
    .Events(new SomeEvent("value"), new SomeOtherEvent(42));
```

Chain multiple `ForEventSource` calls to seed different event sources:

```csharp
await scenario.Given
    .ForEventSource(orderId)
    .Events(new OrderCreated("order-1"), new ItemAdded(9.99m));

await scenario.Given
    .ForEventSource(anotherOrderId)
    .Events(new OrderCreated("order-2"));
```

**Rules:**
- Call `Given` before asserting — events are processed in the order they are supplied.
- Do not put the act-under-test inside `Given`; the `Given` call **is** the act for a read model spec.

---

## Step 3 — Assert on Instance

```csharp
_scenario.Instance!.Total.ShouldEqual(14.49m);
_scenario.Instance!.Name.ShouldEqual("Widget");
_scenario.Instance.ShouldNotBeNull();
```

---

## Auto-detection order

`ReadModelScenario<TReadModel>` searches loaded assemblies for a handler in this order:

1. **Reducer** — a class implementing `IReducerFor<TReadModel>`
2. **Fluent projection** — a class implementing `IProjectionFor<TReadModel>`
3. **Model-bound projection** — `TReadModel` carries `[FromEvent<T>]` or `[Key]` attributes

If none is found, `NoReadModelHandlerFound` is thrown.

---

## Example: reducer

```csharp
public class when_items_are_added_to_an_order : Specification
{
    ReadModelScenario<OrderSummary> _scenario;
    static readonly string OrderId = "order-1";

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrderSummary>();
        await _scenario.Given
            .ForEventSource(OrderId)
            .Events(
                new OrderCreated(OrderId),
                new ItemAdded(9.99m),
                new ItemAdded(4.50m));
    }

    [Fact] void should_sum_the_item_prices() => _scenario.Instance!.Total.ShouldEqual(14.49m);
    [Fact] void should_have_the_correct_order_id() => _scenario.Instance!.OrderId.ShouldEqual(OrderId);
}
```

---

## Example: fluent projection

```csharp
public class when_a_product_is_created_and_stock_is_adjusted : Specification
{
    ReadModelScenario<ProductView> _scenario;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<ProductView>();
        await _scenario.Given
            .ForEventSource("product-1")
            .Events(
                new ProductCreated("Widget"),
                new StockAdjusted(100));
    }

    [Fact] void should_set_the_name() => _scenario.Instance!.Name.ShouldEqual("Widget");
    [Fact] void should_set_the_stock() => _scenario.Instance!.Stock.ShouldEqual(100);
}
```

---

## Example: model-bound projection

```csharp
public class when_a_shipment_is_dispatched_and_delivered : Specification
{
    ReadModelScenario<DeliveryStatus> _scenario;
    static readonly DateTimeOffset DeliveredAt = DateTimeOffset.UtcNow;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<DeliveryStatus>();
        await _scenario.Given
            .ForEventSource("shipment-1")
            .Events(
                new ShipmentDispatched("FedEx"),
                new ShipmentDelivered(DeliveredAt));
    }

    [Fact] void should_set_the_carrier() => _scenario.Instance!.Carrier.ShouldEqual("FedEx");
    [Fact] void should_record_the_delivery_time() => _scenario.Instance!.DeliveredAt.ShouldEqual(DeliveredAt);
}
```

---

## Example: initial state (baseline)

```csharp
public class when_an_item_is_added_to_a_cart_with_existing_items : Specification
{
    ReadModelScenario<CartSummary> _scenario;

    async Task Establish()
    {
        var initial = new CartSummary { ItemCount = 3 };
        _scenario = new ReadModelScenario<CartSummary>(initial);
        await _scenario.Given
            .ForEventSource("cart-1")
            .Events(new ItemAddedToCart("item-4"));
    }

    [Fact] void should_increment_the_item_count() => _scenario.Instance!.ItemCount.ShouldEqual(4);
}
```

---

## Checklist

- [ ] New `ReadModelScenario<T>` instance per test (never shared state)
- [ ] `Given.ForEventSource(id).Events(...)` called before asserting
- [ ] Each test asserts a single outcome (one `should_` per behavior)
- [ ] `Instance` is accessed with `!` only after `Given` has been called
- [ ] Run `dotnet build` and `dotnet test` — fix all failures before completing
