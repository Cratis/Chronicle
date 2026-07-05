```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record ChoosingStyleBookStatusModelBound(
    [Key] string Id,

    [SetFrom<ChoosingStyleBookRegistered>] string Title,
    [SetFrom<ChoosingStyleBookRegistered>] string Isbn,

    [SetValue<ChoosingStyleBookBorrowed>(true)]
    [SetValue<ChoosingStyleBookReturned>(false)]
    bool IsBorrowed,

    [SetFrom<ChoosingStyleBookBorrowed>(nameof(ChoosingStyleBookBorrowed.MemberName))]
    [SetValue<ChoosingStyleBookReturned>(null!)] // null! - SetValue's constructor takes non-nullable object
    string? BorrowedBy);
```
