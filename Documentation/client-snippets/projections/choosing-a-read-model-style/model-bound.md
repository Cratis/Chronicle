```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record ChoosingStyleBookStatusModelBound(
    [Key] string Id,

    [SetFrom<ChoosingStyleBookRegistered>] string? Title,
    [SetFrom<ChoosingStyleBookRegistered>] string? Isbn,

    [SetValue<ChoosingStyleBookRegistered>(false)]
    [SetValue<ChoosingStyleBookBorrowed>(true)]
    [SetValue<ChoosingStyleBookReturned>(false)]
    bool IsBorrowed,

    [PII]
    [SetFrom<ChoosingStyleBookBorrowed>(nameof(ChoosingStyleBookBorrowed.MemberName))]
    [ClearWith<ChoosingStyleBookRegistered>]
    [ClearWith<ChoosingStyleBookReturned>]
    string? BorrowedBy);
```
