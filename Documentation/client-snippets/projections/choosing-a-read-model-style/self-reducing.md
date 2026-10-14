```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

public record ChoosingStyleBookStatusSelfReducing(
    string Id,
    string? Title,
    string? Isbn,
    bool IsBorrowed,
    [PII] string? BorrowedBy)
{
    public static ChoosingStyleBookStatusSelfReducing Reduce(
        ChoosingStyleBookRegistered @event,
        ChoosingStyleBookStatusSelfReducing? current,
        EventContext context) =>
        new(context.EventSourceId.Value, @event.Title, @event.Isbn, false, null);

    public static ChoosingStyleBookStatusSelfReducing Reduce(
        ChoosingStyleBookBorrowed @event,
        ChoosingStyleBookStatusSelfReducing? current,
        EventContext context) =>
        (current ?? new(context.EventSourceId.Value, null, null, false, null)) with
        {
            IsBorrowed = true,
            BorrowedBy = @event.MemberName
        };

    public static ChoosingStyleBookStatusSelfReducing Reduce(
        ChoosingStyleBookReturned @event,
        ChoosingStyleBookStatusSelfReducing? current,
        EventContext context) =>
        (current ?? new(context.EventSourceId.Value, null, null, false, null)) with
        {
            IsBorrowed = false,
            BorrowedBy = null
        };
}
```
