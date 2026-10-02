```csharp
var result = await reactors.ClearFailedPartitionQuarantineFor<MyReactor>(partition, retryImmediately: true);

// result.Outcome:      Cleared, NotFound or NotQuarantined
// result.RetryOutcome: Started, ObserverQuarantined, ... (only meaningful when retryImmediately is true)
```
