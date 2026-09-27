```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record TroubleshootingInvitationAccepted(string Email);

public class TroubleshootingInvitationMailReactor : IReactor
{
    public Task Accepted(TroubleshootingInvitationAccepted @event) => Task.CompletedTask;
}

public class TroubleshootingReactorPartitionRetry
{
    public async Task Run(IEventStore eventStore)
    {
        var failedPartitions = await eventStore.Reactors.GetFailedPartitionsFor<TroubleshootingInvitationMailReactor>();
        var failedPartition = failedPartitions.FirstOrDefault();
        if (failedPartition is not null)
        {
            var outcome = await eventStore.Reactors.RetryFailedPartitionFor<TroubleshootingInvitationMailReactor>(failedPartition.Partition);
            // Handle outcome before reporting recovery as successful.
        }
    }
}
```
