```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique]
public record UcUserRegisteredOnce(string Email, string DisplayName);
```
