# Filter reducers and reactors by tag

Use `[FilterEventsByTag]` when a reducer or reactor should only handle events that carry specific tags.

## What counts as a matching tag

Chronicle compares the filter tag against the tags on the appended event. Those tags can come from:

- `[Tag]` or `[Tags]` on the event type
- The `tags:` argument when you append the event

`[Tag]` and `[Tags]` on the reducer or reactor do not filter anything. They only label the observer itself.

## Filter a reactor by tag

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
[Tag("customer-lifecycle")]
public record CustomerRegistered(string EmailAddress);

public class CustomerRegistrationService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new CustomerRegistered(emailAddress),
            tags: ["vip", "onboarding"]);
}

[FilterEventsByTag("vip")]
public class VipWelcomeReactor : IReactor
{
    public Task Registered(CustomerRegistered @event, EventContext context) => Task.CompletedTask;
}
```

The reactor receives the event because the append call added the `vip` tag.

## Filter a reducer by tag

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record OrderPlaced(decimal TotalAmount);

public record PriorityOrderTotals(decimal TotalAmount);

[FilterEventsByTag("priority")]
public class PriorityOrderTotalsReducer : IReducerFor<PriorityOrderTotals>
{
    public PriorityOrderTotals Placed(OrderPlaced @event, PriorityOrderTotals? current, EventContext context) =>
        new((current?.TotalAmount ?? 0m) + @event.TotalAmount);
}

public class CheckoutService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new OrderPlaced(totalAmount),
            tags: ["priority"]);
}
```

The reducer only updates when the appended event carries the `priority` tag.

## Use multiple filter tags

Multiple `[FilterEventsByTag]` attributes widen the match:

```csharp
[FilterEventsByTag("vip")]
[FilterEventsByTag("priority")]
public class PriorityNotificationsReactor : IReactor
{
    public Task Registered(CustomerRegistered @event) => Task.CompletedTask;
}
```

Chronicle dispatches the event if it has either `vip` or `priority`.
