# Events — Reference

## [EventType] attribute

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record OrderPlaced(string CustomerId, decimal Total);

// Optional: stable custom ID (survives class renames)
[EventType("4f8e91a1-0e3e-4c12-933c-b7c8c26a0de1")]
public record OrderPlaced(string CustomerId, decimal Total);
```

Every event must be decorated with `[EventType]` — this makes it discoverable and registers its schema with Chronicle.

---

## Good event design

- **Past tense**: `OrderPlaced`, `UserOnboarded`, `BookReturned` ✓ — not `PlaceOrder`, `OnboardUser`
- **One purpose**: an `AddressChanged` event should only carry address fields, not payment info
- **No nullables** unless the field is genuinely optional (e.g. `string? MiddleName`)
- **Immutable facts**: events represent something that *has happened* — do not use them to encode intent or possibility

---

## Appending an event

Inject `IEventLog` and call `Append(eventSourceId, eventInstance)`:

```csharp
public class OrdersController(IEventLog eventLog) : ControllerBase
{
    [HttpPost]
    public async Task PlaceOrder([FromBody] PlaceOrder command)
    {
        var result = await eventLog.Append(
            command.OrderId,
            new OrderPlaced(command.CustomerId, command.Total));

        if (!result.IsSuccess)
        {
            // result.HasConcurrencyViolation — two requests raced
            // result.HasConstraintViolations — uniqueness constraint failed
        }
    }
}
```

The first argument is the **event source ID** — the identity the event belongs to (analogous to an aggregate root ID). Projections and reducers are keyed by this ID by default.

---

## Constraints (uniqueness)

Enforce uniqueness at append time without application-level checks:

```csharp
public class UniqueOrderNumberConstraint : IUniqueConstraintFor<OrderPlaced>
{
    public string GetConstraintValue(OrderPlaced @event) => @event.OrderNumber;
}
```

Violation surfaces in `AppendResult.HasConstraintViolations`.

---

## Tags

```csharp
[EventType]
[EventTag("high-value")]
public record LargeOrderPlaced(decimal Total);

// Or apply at append time:
await eventLog.Append(orderId, new OrderPlaced(...), ["high-value"]);
```

Tags allow filtering reactors, projections and reducers to only handle tagged events.
