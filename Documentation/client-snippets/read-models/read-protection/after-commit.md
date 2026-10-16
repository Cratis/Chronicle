```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

public static class ProtectedStreamClosing
{
    public static Task CloseAfterCommit(ReadProtectionFrame frame, EventStreamType streamType, EventStreamId streamId) =>
        frame.RunAfterCommit(sequence => sequence.CompleteStream(streamType, streamId));
}
```
