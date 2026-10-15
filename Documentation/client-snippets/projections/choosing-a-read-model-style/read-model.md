```csharp
using Cratis.Chronicle.Compliance.GDPR;

public record ChoosingStyleBookStatus(
    string Id,
    string? Title,
    string? Isbn,
    bool IsBorrowed,
    [PII] string? BorrowedBy);
```
