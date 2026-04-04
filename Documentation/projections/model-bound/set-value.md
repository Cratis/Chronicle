# Setting Constant Values

The `SetValue` attribute sets a property to a compile-time constant whenever an event of a specified type occurs. Use it when a property should take a fixed value in response to an event — for example, setting a status flag, assigning a default category, or recording a fixed version number.

## Basic Usage

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlaced(string CustomerName);

[EventType]
public record OrderCanceled;

public record Order(
    [Key]
    Guid Id,

    [SetFrom<OrderPlaced>(nameof(OrderPlaced.CustomerName))]
    string CustomerName,

    [SetValue<OrderPlaced>("active")]
    [SetValue<OrderCanceled>("canceled")]
    string Status);
```

When an `OrderPlaced` event occurs, `Status` is set to `"active"`. When an `OrderCanceled` event occurs, `Status` is set to `"canceled"`.

## Supported Value Types

`SetValue` accepts any compile-time constant: strings, integers, longs, doubles, booleans, and enum values.

```csharp
[EventType]
public record ThingHappened;

public record Thing(
    [Key]
    Guid Id,

    [SetValue<ThingHappened>("pending")]
    string StatusLabel,

    [SetValue<ThingHappened>(42)]
    int Priority,

    [SetValue<ThingHappened>(true)]
    bool IsActive,

    [SetValue<ThingHappened>(3.14)]
    double Score);
```

## Multiple Events

Apply the attribute more than once to respond to multiple event types:

```csharp
[EventType]
public record SubscriptionStarted;

[EventType]
public record SubscriptionPaused;

[EventType]
public record SubscriptionCanceled;

public record Subscription(
    [Key]
    Guid Id,

    [SetValue<SubscriptionStarted>("active")]
    [SetValue<SubscriptionPaused>("paused")]
    [SetValue<SubscriptionCanceled>("canceled")]
    string State);
```

## SetValue vs SetFrom

| Attribute | Source | When to use |
|---|---|---|
| `SetValue<TEvent>(value)` | Compile-time constant | The value does not come from the event payload |
| `SetFrom<TEvent>(property)` | Event property | The value comes from a property on the event |

Combine both on the same read model when different properties come from different sources:

```csharp
public record Invoice(
    [Key]
    Guid Id,

    [SetFrom<InvoiceIssued>(nameof(InvoiceIssued.Amount))]
    decimal Amount,

    [SetValue<InvoiceIssued>("issued")]
    [SetValue<InvoicePaid>("paid")]
    string Status);
```
