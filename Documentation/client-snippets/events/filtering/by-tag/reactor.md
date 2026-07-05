```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
[Tag("customer-lifecycle")]
public record FilterByTagCustomerRegistered(string EmailAddress);

public class FilterByTagCustomerRegistrationService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterByTagCustomerRegistered(emailAddress),
            tags: ["vip", "onboarding"]);
}

[FilterEventsByTag("vip")]
public class FilterByTagVipWelcomeReactor : IReactor
{
    public Task Registered(FilterByTagCustomerRegistered @event, EventContext context) => Task.CompletedTask;
}
```
