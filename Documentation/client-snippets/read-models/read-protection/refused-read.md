```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

public static class ProtectedScreenData
{
    // Uses ProtectedSeatReservations from the owner-commit example.
    // Inside a protected invocation this plain read is refused with ReadRefusedWhileReadsAreProtected,
    // because it would feed a decision without guarding it. Opening an advisory allowance says that
    // a stale value is acceptable here, and the reason is required.
    public static async Task<int> SeatsShownOnScreen(IEventStore eventStore, IUnitOfWork unitOfWork, ReadModelKey flightId)
    {
        using (unitOfWork.AllowReads(ReadAllowance.Advisory, "Shown on the booking screen. Not used for the decision."))
        {
            var view = await eventStore.ReadModels.GetInstanceById<ProtectedSeatReservations>(flightId);
            return view?.Seats ?? 0;
        }
    }
}
```
