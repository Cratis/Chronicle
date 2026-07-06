```csharp title="Nested object events"
using Cratis.Chronicle.Events;

[EventType]
public record SliceCreatedForNestedEvents(string Name);

[EventType]
public record CommandSetForNestedEvents(string Name, string Schema);

[EventType]
public record CommandClearedForNestedEvents;
```
