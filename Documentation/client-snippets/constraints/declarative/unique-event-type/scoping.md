```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeScopedLoanCheckedOut(string Title);

[EventType]
public record ConstraintsUniqueEventTypeScopedLoanReturned;

public class ConstraintsUniqueEventTypeOneOpenLoanPerBranch : IConstraint
{
    // One open loan per borrower per branch. The stream the event is appended to decides
    // which cycle it belongs to, so the same borrower can hold one open loan at every
    // branch, and returning at one branch opens the next cycle only there.
    public void Define(IConstraintBuilder builder) =>
        builder
            .PerEventStreamId()
            .Unique<ConstraintsUniqueEventTypeScopedLoanCheckedOut>()
            .RemovedWith<ConstraintsUniqueEventTypeScopedLoanReturned>();
}
```
