```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class EventsIndexEmployeesService(IEventLog eventLog)
{
    public Task RegisterEmployee(EventSourceId employeeId, string firstName, string lastName) =>
        eventLog.Append(employeeId, new EventsIndexEmployeeRegistered(firstName, lastName));
}
```
