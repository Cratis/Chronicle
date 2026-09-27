```csharp
using Cratis.Chronicle.Reactors;

var failedPartitions = await eventStore.Reactors.GetFailedPartitionsFor<InvitationMailReactor>();
var failedPartition = failedPartitions.FirstOrDefault();
if (failedPartition is not null)
{
    var outcome = await eventStore.Reactors.RetryFailedPartitionFor<InvitationMailReactor>(failedPartition.Partition);
    // Handle outcome before reporting recovery as successful.
}
```
