# Getting Started with Reactors

Reactors provide a straightforward way to execute side effects when events are appended. This guide walks through creating your first reactor and registering it for discovery.

## Prerequisites

Before you begin, ensure you have:

- A Chronicle-enabled application
- A basic understanding of events and event sourcing
- At least one event type marked with `[EventType]`

## Creating a Reactor

### 1. Define an Event

```csharp
[EventType]
public record OrderPlaced(Guid OrderId, string CustomerEmail, decimal TotalAmount);
```

### 2. Implement the Reactor

Create a class that implements `IReactor` and add one or more handler methods that match supported signatures:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class OrderNotificationsReactor : IReactor
{
    public Task Placed(OrderPlaced @event, EventContext context)
    {
        return NotifyAsync(@event.CustomerEmail, @event.TotalAmount, context.Occurred);
    }

    Task NotifyAsync(string email, decimal amount, DateTimeOffset occurred) => Task.CompletedTask;
}
```

### 3. Register the Reactor (Optional)

If your application uses dependency injection, ensure the reactor can be created by the configured service provider. For example, register it as a transient service in your DI container.

### 4. Customize with the Reactor Attribute (Optional)

You can override the default identifier or event sequence using the `[Reactor]` attribute:

```csharp
using Cratis.Chronicle.Reactors;

[Reactor(id: "order-notifications", eventSequence: "orders")]
public class OrderNotificationsReactor : IReactor
{
    public Task Placed(OrderPlaced @event) => Task.CompletedTask;
}
```

## Method Signatures

Reactor methods are discovered by convention. For a full list of supported signatures and guidance, see [Event Processing](event-processing.md).

## Next Steps

- Explore [Event Processing](event-processing.md) for method patterns and error handling
- Learn about [Tagging Reactors](../concepts/tagging-reactors.md) to organize reactors

