```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringCustomerRegistered(string EmailAddress);

public class ReactorsFilteringCustomerService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReactorsFilteringCustomerRegistered(emailAddress),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class ReactorsFilteringCustomerWelcomeReactor : IReactor
{
    public Task Registered(ReactorsFilteringCustomerRegistered @event, EventContext context) =>
        Task.CompletedTask;
}
```
