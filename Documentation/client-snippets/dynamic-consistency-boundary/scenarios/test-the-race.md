```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

public static class LoanRaceCheck
{
    // Returns true when the guard rejected the second writer. Run it against a test event store
    // that already holds a registered member.
    public static async Task<bool> SecondWriterIsRejected(IEventStore eventStore, Guid memberId)
    {
        var member = await eventStore.GetDecisionReads().GetDetached<MemberLoans>(memberId);

        // A competing writer appends after the read and before the guarded append.
        await eventStore.EventLog.Append(memberId, new LoanTaken("978-0-13-468599-1"));

        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(memberId, new LoanTaken("978-0-596-52068-7"))],
            guardedBy: [member]);

        return result.GetDecisionConflicts([member]).Any();
    }
}
```
