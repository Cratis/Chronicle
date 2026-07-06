```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record FilterByTagMultiCustomerRegistered(string EmailAddress);

[FilterEventsByTag("vip")]
[FilterEventsByTag("priority")]
public class FilterByTagMultiPriorityNotificationsReactor : IReactor
{
    public Task Registered(FilterByTagMultiCustomerRegistered @event) => Task.CompletedTask;
}
```
