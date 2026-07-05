```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned;
```
