```csharp
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<BookAdded>]
public record Book(
    BookId Id,

    string Title,
    string Isbn,

    [SetValue<BookAdded>(false)]
    [SetValue<BookBorrowed>(true)]
    [SetValue<BookReturned>(false)]
    bool OnLoan,

    [SetFrom<BookBorrowed>(nameof(BookBorrowed.MemberName))]
    string? BorrowedBy);
```
