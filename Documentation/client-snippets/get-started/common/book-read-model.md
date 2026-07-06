```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<GetStartedBookAdded>]
public record GetStartedBook(
    [Key]
    Guid Id,

    string Title,

    string Isbn,

    [SetValue<GetStartedBookAdded>(false)]
    [SetValue<GetStartedBookBorrowed>(true)]
    [SetValue<GetStartedBookReturned>(false)]
    bool OnLoan,

    [SetFrom<GetStartedBookBorrowed>(nameof(GetStartedBookBorrowed.MemberName))]
    string? BorrowedBy);
```
