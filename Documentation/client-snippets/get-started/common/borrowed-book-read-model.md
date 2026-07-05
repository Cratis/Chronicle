```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<GetStartedBookBorrowed>]
[RemovedWith<GetStartedBookReturned>]
public record GetStartedBorrowedBook(
    [Key]
    Guid Id,

    string MemberName);
```
