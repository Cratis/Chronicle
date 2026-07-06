```csharp title="Append child event with parent key"
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class GroupMembershipWithEventParentKey(IEventStore eventStore)
{
    public Task AddUserToGroup(EventSourceId userId, string groupId, string role) =>
        eventStore.EventLog.Append(userId, new UserAddedWithEventParentKey(groupId, userId.Value, role));
}
```
