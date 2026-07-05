```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record FilterBySourceTypeCustomerRegistered(string EmailAddress);

public class FilterBySourceTypeCustomerRegistrationService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterBySourceTypeCustomerRegistered(emailAddress),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class FilterBySourceTypeCustomerWelcomeReactor : IReactor
{
    public Task Registered(FilterBySourceTypeCustomerRegistered @event, EventContext context) => Task.CompletedTask;
}
```
