```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

[EventType]
public record ProtectedSeatReserved(int Seats);

[Passive]
public record ProtectedSeatReservations(
    string Id,
    [AddFrom<ProtectedSeatReserved>(nameof(ProtectedSeatReserved.Seats))] int Seats);

public static class ProtectedReservation
{
    public static async Task<bool> Reserve(IEventStore eventStore, EventSourceId flightId)
    {
        var unitOfWork = (UnitOfWork)eventStore.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unitOfWork.EnforceReadProtection();
        var owner = unitOfWork.ClaimDecisionReadCommitOwnership();
        try
        {
            var reservations = await eventStore.GetDecisionReads()
                .GetFor<ProtectedSeatReservations>(unitOfWork, flightId.Value);
            if ((reservations.Instance?.Seats ?? 0) >= 180)
            {
                await unitOfWork.RollbackAsOwner(owner);
                return false;
            }

            unitOfWork.AddEvent(EventSequenceId.Log, flightId, new ProtectedSeatReserved(1), Causation.Unknown());
            await unitOfWork.CommitAsOwner(owner);

            // A changed read is reported as a conflict: nothing was appended. Re-read and decide again.
            return !unitOfWork.GetDecisionConflicts().Any() && unitOfWork.GetConcurrencyViolations().Count() == 0;
        }
        catch
        {
            if (!unitOfWork.IsCompleted) await unitOfWork.RollbackAsOwner(owner);
            throw;
        }
    }
}
```
