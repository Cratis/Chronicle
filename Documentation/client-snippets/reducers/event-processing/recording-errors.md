```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingInvalidDataDetected(string Reason);

public record EventProcessingValidationResult(bool IsValid, List<string> Errors);

public class EventProcessingValidationResultReducer : IReducerFor<EventProcessingValidationResult>
{
    public EventProcessingValidationResult Detected(EventProcessingInvalidDataDetected @event, EventProcessingValidationResult? current)
    {
        var errors = new List<string>(current?.Errors ?? []) { @event.Reason };

        return new EventProcessingValidationResult(false, errors);
    }
}
```
