```csharp
using Cratis.Chronicle.Events;

[EventType]
public record PdlAutoMapUserRegistered(string Name, string Email, int Age);
```
