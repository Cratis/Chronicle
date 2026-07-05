```csharp
using Cratis.Chronicle.Events;

[EventType]
public record ChoosingStyleBookRegistered(string Title, string Isbn);

[EventType]
public record ChoosingStyleBookBorrowed(string MemberName);

[EventType]
public record ChoosingStyleBookReturned;
```
