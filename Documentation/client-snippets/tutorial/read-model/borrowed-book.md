```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<BookBorrowed>]
[RemovedWith<BookReturned>]
public record BorrowedBook(
    [Key]
    BookId Id,

    string MemberName);
```
