```csharp title="Append child event to parent"
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class GroupMembershipWithDefaultParentKey(IEventStore eventStore)
{
    public Task AddUserToGroup(EventSourceId groupId, string userId, string role) =>
        eventStore.EventLog.Append(groupId, new UserAddedWithDefaultParentKey(userId, role));
}
```
