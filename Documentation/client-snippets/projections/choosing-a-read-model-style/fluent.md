```csharp
using Cratis.Chronicle.Projections;

public record ChoosingStyleBookStatusFluent(
    string Id,
    string Title,
    string Isbn,
    bool IsBorrowed,
    string? BorrowedBy);

public class ChoosingStyleBookStatusProjection : IProjectionFor<ChoosingStyleBookStatusFluent>
{
    public void Define(IProjectionBuilderFor<ChoosingStyleBookStatusFluent> builder) => builder
        .From<ChoosingStyleBookRegistered>(_ => _
            .Set(m => m.Id).ToEventSourceId()
            .Set(m => m.Title).To(e => e.Title)
            .Set(m => m.Isbn).To(e => e.Isbn)
            .Set(m => m.IsBorrowed).ToValue(false)
            .Set(m => m.BorrowedBy).ToValue(null))
        .From<ChoosingStyleBookBorrowed>(_ => _
            .Set(m => m.IsBorrowed).ToValue(true)
            .Set(m => m.BorrowedBy).To(e => e.MemberName))
        .From<ChoosingStyleBookReturned>(_ => _
            .Set(m => m.IsBorrowed).ToValue(false)
            .Set(m => m.BorrowedBy).ToValue(null));
}
```
