```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record UcUserRegistered([property: Unique(name: "UniqueEmail")] string Email, string DisplayName);
```
