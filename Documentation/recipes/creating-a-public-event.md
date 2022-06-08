# Creating a public event

Public events are used as the contract a microservice wants to support outside of itself.
They can either be manually appended to the **outbox** or derived from the private events
in the **event log**.

It is much like [creating an event](./creating-an-event.md) with a couple of exceptions.
Conceptually one typically needs to think more about the concept of idempotency. Which
basically means that the outside world shouldn't need to piece together what sits on the
inside of your microservice in the same order / sequence to get the same result.
Your microservice should therefor take the responsibility of doing this heavy lifting.

The **outbox** has a different characteristic than the **event log** as well. Instead of being
append only, it has a retention policy to it that makes it only keep one instance per unique
identifier (`EventSourceId`) per event type. That outside world can therefor not reason
about its history.

With C# you define a public event in the same as a private event, only difference is the
`isPublic: true` property on the `[EventType]` attribute.

```csharp
[EventType("cf0a9242-706e-4293-bd3d-e795b9348bd6", isPublic: true)]
public record AccountBalance(double Balance);
```

Only public events can be appended to the **outbox**, private events will cause an exception.
