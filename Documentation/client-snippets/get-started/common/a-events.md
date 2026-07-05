```csharp
using Cratis.Chronicle.Events;

[EventType]
public record GetStartedBookAdded(string Title, string Isbn);

[EventType]
public record GetStartedBookBorrowed(string MemberName);

[EventType]
public record GetStartedBookReturned;
```
