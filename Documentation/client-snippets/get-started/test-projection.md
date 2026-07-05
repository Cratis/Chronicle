```csharp title="The projection - builds queryable state"
[FromEvent<TestEvent>]
public record TestProjection(
    string Message,
    [SetFromContext<TestEvent>(nameof(EventContext.EventSourceId))] string EventSource);
```
