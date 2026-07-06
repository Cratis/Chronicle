```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record ChoosingStyleBookStatusReducerModel(
    string Id,
    string Title,
    string Isbn,
    bool IsBorrowed,
    string? BorrowedBy);

public class ChoosingStyleBookStatusReducer : IReducerFor<ChoosingStyleBookStatusReducerModel>
{
    public ChoosingStyleBookStatusReducerModel OnBookRegistered(
        ChoosingStyleBookRegistered @event,
        ChoosingStyleBookStatusReducerModel? current,
        EventContext context) =>
        new(
            Id: context.EventSourceId.Value,
            Title: @event.Title,
            Isbn: @event.Isbn,
            IsBorrowed: false,
            BorrowedBy: null);

    public ChoosingStyleBookStatusReducerModel OnBookBorrowed(
        ChoosingStyleBookBorrowed @event,
        ChoosingStyleBookStatusReducerModel? current,
        EventContext context) =>
        current! with
        {
            IsBorrowed = true,
            BorrowedBy = @event.MemberName
        };

    public ChoosingStyleBookStatusReducerModel OnBookReturned(
        ChoosingStyleBookReturned @event,
        ChoosingStyleBookStatusReducerModel? current,
        EventContext context) =>
        current! with
        {
            IsBorrowed = false,
            BorrowedBy = null
        };
}
```
