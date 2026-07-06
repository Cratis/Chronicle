```csharp
using Cratis.Chronicle.Events;

public record ModelingEventsAddress(string Street, string City);

// A fact that happened
[EventType]
public record ModelingEventsAddressChanged(ModelingEventsAddress Address);

// An intent (that's a command) or a state blob (that's a read model) — not an event
[EventType]
public record ModelingEventsUpdateAddress(ModelingEventsAddress Address);
```
