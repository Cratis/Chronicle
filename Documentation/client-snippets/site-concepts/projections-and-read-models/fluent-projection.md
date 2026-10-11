```csharp
using Cratis.Chronicle.Projections;

public class BookStatusProjection : IProjectionFor<BookStatus>
{
    public void Define(IProjectionBuilderFor<BookStatus> builder) => builder
        .From<BookRegistered>(_ => _
            .Set(m => m.Id).ToEventSourceId()
            .Set(m => m.IsBorrowed).ToValue(false)
            .Clear(m => m.BorrowedBy))
        .From<BookBorrowed>(_ => _
            .Set(m => m.IsBorrowed).ToValue(true)
            .Set(m => m.BorrowedBy).To(e => e.MemberName))
        .From<BookReturned>(_ => _
            .Set(m => m.IsBorrowed).ToValue(false)
            .Clear(m => m.BorrowedBy));
}
```
