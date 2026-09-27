```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

public static class ReactorPartitionRecovery
{
    public static async Task<ReactorPartitionRetryOutcome?> RetryOne<TReactor>(IEventStore eventStore)
        where TReactor : IReactor
    {
        var failedPartitions = await eventStore.Reactors.GetFailedPartitionsFor<TReactor>();
        var failedPartition = failedPartitions.FirstOrDefault();
        if (failedPartition is null) return null;

        return await eventStore.Reactors.RetryFailedPartitionFor<TReactor>(failedPartition.Partition);
    }
}
```

In .NET, `ReactorPartitionRetryOutcome.Started` means a recovery job started or resumed. `PartitionNotFound` means the failure was already cleared (or never existed), not that the call threw. `ObserverQuarantined` and `PartitionQuarantined` mean retry was refused until the corresponding quarantine is cleared. `Unknown` means the server returned an outcome this client version does not recognize; do not treat it as success.
