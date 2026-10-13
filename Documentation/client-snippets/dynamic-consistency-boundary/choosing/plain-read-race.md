```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

[EventType]
public record SeatClaimed(string ClaimedBy);

public record SeatAvailability(Guid Id, bool Claimed);

public static class ClaimSeatWithAPlainRead
{
    // Do not use this to make a rule hold. Two requests can both read Claimed = false
    // and both append. The read guards nothing.
    public static async Task<bool> Claim(IReadModels readModels, IEventLog eventLog, Guid seatId, string claimedBy)
    {
        var seat = await readModels.GetInstanceById<SeatAvailability>(seatId);
        if (seat.Claimed)
        {
            return false;
        }

        var result = await eventLog.Append(seatId, new SeatClaimed(claimedBy));
        return result.IsSuccess;
    }
}
```
