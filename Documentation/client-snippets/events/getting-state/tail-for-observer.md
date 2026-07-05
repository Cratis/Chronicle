```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class GettingStateObserverProgress(IEventSequence eventSequence)
{
    public async Task<EventSequenceNumber> GetRelevantTail(Type observerType)
    {
        // Uses the observer's event type filters to compute the relevant tail.
        return await eventSequence.GetTailSequenceNumberForObserver(observerType);
    }
}
```
