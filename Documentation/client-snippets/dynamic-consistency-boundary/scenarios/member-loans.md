```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record MemberRegistered(string Name);

[EventType]
public record LoanTaken(string Isbn);

[EventType]
public record LoanReturned(string Isbn);

[FromEvent<MemberRegistered>]
public record MemberLoans(
    Guid Id,
    [Increment<LoanTaken>]
    [Decrement<LoanReturned>]
    int Active);

public enum TakeLoanOutcome
{
    Taken,
    UnknownMember,
    LimitReached,
    ChangedConcurrently,
    Failed
}

public static class TakeLoan
{
    const int MaxActiveLoans = 3;

    public static async Task<TakeLoanOutcome> Handle(IEventStore eventStore, Guid memberId, string isbn)
    {
        var member = await eventStore.GetDecisionReads().GetDetached<MemberLoans>(memberId);
        if (member.Instance is not { } loans)
        {
            return TakeLoanOutcome.UnknownMember;
        }

        if (loans.Active >= MaxActiveLoans)
        {
            return TakeLoanOutcome.LimitReached;
        }

        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(memberId, new LoanTaken(isbn))],
            guardedBy: [member]);

        if (result.GetDecisionConflicts([member]).Any())
        {
            // Another loan or return was appended for this member after the read. Nothing was appended.
            return TakeLoanOutcome.ChangedConcurrently;
        }

        return result.IsSuccess ? TakeLoanOutcome.Taken : TakeLoanOutcome.Failed;
    }
}
```
