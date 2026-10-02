```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

var failedPartitions = await eventStore.Reactors.GetFailedPartitionsFor<InvitationMailReactor>();
var quarantined = failedPartitions.FirstOrDefault(partition => partition.IsQuarantined == true);
if (quarantined is not null)
{
    var result = await eventStore.Reactors.ClearFailedPartitionQuarantineFor<InvitationMailReactor>(
        quarantined.Partition,
        retryImmediately: true);
    // Handle result.Outcome and result.RetryOutcome before reporting recovery as successful.
}
```

In .NET, `ReactorPartitionQuarantineClearOutcome.Cleared` means the quarantine was cleared and the partition has a new retry budget; its attempt history is kept. `NotFound` means the partition is not among the failed partitions, and `NotQuarantined` means it is failed but was not quarantined; neither changed anything. `Unknown` means the server returned an outcome this client version does not recognize. `RetryOutcome` is only meaningful when `retryImmediately` is `true` and the quarantine was cleared: `ObserverQuarantined` there means the partition was cleared but the retry did not start because the observer itself is quarantined.
