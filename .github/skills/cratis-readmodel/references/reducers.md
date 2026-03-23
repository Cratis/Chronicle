# Reducers — Reference

## Signatures

All public methods with a `[EventType]` record as the first parameter are treated as event handlers. All the following signatures are valid:

```csharp
public TState Handle(TEvent @event, TState? current, EventContext context)
public TState Handle(TEvent @event, TState? current)
public Task<TState> Handle(TEvent @event, TState? current, EventContext context)
public Task<TState> Handle(TEvent @event, TState? current)
```

Method names are yours to choose — Chronicle matches by the event type parameter.

---

## EventContext properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `context.Occurred` | `DateTimeOffset` | When the event was appended |
| `context.EventSourceId` | `EventSourceId` | The aggregate root identifier |
| `context.SequenceNumber` | `EventSequenceNumber` | Position in the event sequence |
| `context.CorrelationId` | `CorrelationId` | Correlation ID for causality tracking |

---

## Full example: shopping cart

```csharp
public record CartItem(string Sku, int Quantity, decimal UnitPrice);
public record CartState(IReadOnlyList<CartItem> Items, decimal Total, bool IsCheckedOut);

public class CartReducer : IReducerFor<CartState>
{
    public CartState Created(CartCreated @event, CartState? current, EventContext context)
        => new([], 0m, false);

    public CartState ItemAdded(CartItemAdded @event, CartState? current, EventContext context)
    {
        var items = (current?.Items ?? []).ToList();
        var existing = items.FirstOrDefault(i => i.Sku == @event.Sku);
        if (existing is not null)
        {
            items.Remove(existing);
            items.Add(existing with { Quantity = existing.Quantity + @event.Quantity });
        }
        else
        {
            items.Add(new CartItem(@event.Sku, @event.Quantity, @event.UnitPrice));
        }
        var total = items.Sum(i => i.Quantity * i.UnitPrice);
        return new CartState(items, total, false);
    }

    public CartState ItemRemoved(CartItemRemoved @event, CartState? current, EventContext context)
    {
        var items = (current?.Items ?? []).Where(i => i.Sku != @event.Sku).ToList();
        return new CartState(items, items.Sum(i => i.Quantity * i.UnitPrice), false);
    }

    public CartState CheckedOut(CartCheckedOut @event, CartState? current, EventContext context)
        => (current ?? new([], 0m, false)) with { IsCheckedOut = true };
}
```

---

## Passive reducers

A passive reducer is not an active observer — it computes state on demand. Useful for previews or draft calculations:

```csharp
[Passive]
public class DraftOrderReducer : IReducerFor<DraftOrder> { ... }
```

Call explicitly rather than subscribing automatically:

```csharp
var state = await readModels.GetOne<DraftOrder>(orderId);
```

---

## Reading reducer state

```csharp
// Single instance
var cart = await readModels.GetOne<CartState>(cartId);

// All instances
var allCarts = await readModels.GetAll<CartState>();
```
