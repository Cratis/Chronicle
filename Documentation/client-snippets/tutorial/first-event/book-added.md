```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);
```
