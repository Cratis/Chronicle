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

In .NET, `ReactorPartitionRetryOutcome.Started` means a recovery job started or resumed. `PartitionNotFound` means the failure was already cleared (or never existed), not that the call threw. `ObserverQuarantined` and `PartitionQuarantined` mean retry was refused until the corresponding quarantine is cleared. `Unknown` means the server returned an outcome this client version does not recognize; do not treat it as success.
