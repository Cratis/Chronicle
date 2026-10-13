```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record ChoosingStyleBookStatusReducerModel(
    string Id,
    string? Title,
    string? Isbn,
    bool IsBorrowed,
    [PII] string? BorrowedBy);

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
        (current ?? new(context.EventSourceId.Value, null, null, false, null)) with
        {
            IsBorrowed = true,
            BorrowedBy = @event.MemberName
        };

    public ChoosingStyleBookStatusReducerModel OnBookReturned(
        ChoosingStyleBookReturned @event,
        ChoosingStyleBookStatusReducerModel? current,
        EventContext context) =>
        (current ?? new(context.EventSourceId.Value, null, null, false, null)) with
        {
            IsBorrowed = false,
            BorrowedBy = null
        };
}
```
