```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecSimpleUserCreated(string Name, string Email, DateTimeOffset CreatedAt);
```
