# Appending events

Once you have defined the events, you can start using them.
Events represent state changes in your system, and you use them by appending them to an [event sequence](../concepts/event-sequence.md).

Chronicle provides a default event sequence called the **event log**. The **event log** is typically the main sequence you use, similar to the `main` branch of a **Git** repository.

You can access the **event log** through a property on the `IEventStore` type:

```csharp
var eventLog = eventStore.EventLog;
```

The event log provides methods for appending single or multiple events to the event sequence.
In this example, we will use the method for appending a single event.

The following code appends a couple of `UserOnboarded` events to indicate that users have been onboarded to the system.

{{snippet:Quickstart-DemoData-Users}}
