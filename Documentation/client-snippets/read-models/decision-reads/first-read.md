```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

public record WorkshopId(string Value) : EventSourceId(Value);

[EventType]
public record WorkshopOpened(int Places);

[EventType]
public record PlaceBooked(string Attendee);

[EventType]
public record PlaceCancelled(string Attendee);

// The decision model: a flat projection with an Id, keyed by the event source.
[FromEvent<WorkshopOpened>]
public record WorkshopPlaces(
    string Id,
    [SetFrom<WorkshopOpened>(nameof(WorkshopOpened.Places))] int Total,
    [Increment<PlaceBooked>] [Decrement<PlaceCancelled>] int Booked);

public static class BookPlace
{
    public static async Task<bool> Handle(IEventStore eventStore, WorkshopId workshop, string attendee)
    {
        // 1. The read notes the log position, then folds the workshop's events.
        var read = await eventStore.GetDecisionReads().GetDetached<WorkshopPlaces>(new ReadModelKey(workshop.Value));
        if (read.Instance is not { } places || places.Booked >= places.Total)
        {
            return false;
        }

        // 2. The append carries the read as its guard. A later PlaceBooked, PlaceCancelled or
        //    WorkshopOpened for this workshop rejects it and nothing is written.
        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(workshop, new PlaceBooked(attendee))],
            guardedBy: [read]);

        return result.IsSuccess;
    }
}
```
