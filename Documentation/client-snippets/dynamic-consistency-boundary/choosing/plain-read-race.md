```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record SeatOpened(string Row);

[EventType]
public record SeatClaimed(string ClaimedBy);

[FromEvent<SeatOpened>]
public record SeatAvailability(
    Guid Id,
    [Count<SeatClaimed>]
    int Claims);

public static class ClaimSeatWithAPlainRead
{
    // Do not use this to make a rule hold. Two requests can both read Claims = 0
    // and both append. The read guards nothing.
    public static async Task<bool> Claim(IReadModels readModels, IEventLog eventLog, Guid seatId, string claimedBy)
    {
        var seat = await readModels.GetInstanceById<SeatAvailability>(seatId);
        if (seat.Claims > 0)
        {
            return false;
        }

        var result = await eventLog.Append(seatId, new SeatClaimed(claimedBy));
        return result.IsSuccess;
    }
}
```
