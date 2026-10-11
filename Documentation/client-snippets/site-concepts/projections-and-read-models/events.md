```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookRegistered(string Title, string Isbn);

[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned;
```
