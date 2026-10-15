```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;

[EventType]
public record ConcertAnnounced(string Name, int Capacity);

[EventType]
public record SeatReserved(string ReservedBy);

[EventType]
public record ConcertOverbooked(int Excess);

[Passive]
[FromEvent<ConcertAnnounced>]
public record ConcertSeats(
    Guid Id,
    int Capacity,
    [Count<SeatReserved>]
    int Reserved);

public class OverbookingRepair(IReadModels readModels) : IReactor
{
    // A passive read model is folded when it is read, so it includes the reservation that triggered this call.
    public async Task<ConcertOverbooked?> OnSeatReserved(SeatReserved @event, EventContext context)
    {
        var seats = await readModels.GetInstanceById<ConcertSeats>(context.EventSourceId);
        return seats.Reserved > seats.Capacity
            ? new ConcertOverbooked(seats.Reserved - seats.Capacity)
            : null;
    }
}
```
