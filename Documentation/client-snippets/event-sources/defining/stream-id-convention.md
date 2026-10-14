```csharp
using Cratis.Chronicle.Events;

// Chronicle does not impose a format for an event stream id. Keep the convention in one place.
public static class TransactionStreams
{
    public static EventStreamId ForMonth(DateOnly date) => new($"{date.Year:0000}-{date.Month:00}");
}
```
