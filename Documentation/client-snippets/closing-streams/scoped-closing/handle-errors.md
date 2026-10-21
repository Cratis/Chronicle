```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class ScopedClosingErrorHandling(IEventLog eventLog)
{
    public async Task<string> Close(ClosedStreamScope scope)
    {
        var message = string.Empty;
        var result = await eventLog.CompleteStream(scope);

        result.Switch(
            sequenceNumber => message = $"Closed at {sequenceNumber}",
            error => message = error switch
            {
                CompleteStreamError.AlreadyCompleted => "Already closed by an earlier manual close",
                CompleteStreamError.DefaultStreamCannotBeCompleted => "The default stream cannot be closed on its own",
                CompleteStreamError.EmptyScope => "The scope names no dimension",
                CompleteStreamError.ExpectedTailMismatch => "Something landed in the scope since you last looked",
                _ => $"Unexpected error {error}"
            });

        return message;
    }
}
```
