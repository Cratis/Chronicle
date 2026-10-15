```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

[EventType]
public record ChoosingStyleBookRegistered(string Title, string Isbn);

[EventType]
public record ChoosingStyleBookBorrowed([PII] string MemberName);

[EventType]
public record ChoosingStyleBookReturned;
```
