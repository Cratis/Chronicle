```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record BookStatus(
    [Key] string Id,

    [SetFrom<BookRegistered>] string Title,
    [SetFrom<BookRegistered>] string Isbn,

    [SetValue<BookBorrowed>(true)]
    [SetValue<BookReturned>(false)]
    bool IsBorrowed,

    [SetFrom<BookBorrowed>(nameof(BookBorrowed.MemberName))]
    [ClearWith<BookReturned>]
    string? BorrowedBy);
```
