```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeSeveralLoanCheckedOut(string Title);

[EventType]
public record ConstraintsUniqueEventTypeSeveralLoanReturned;

[EventType]
public record ConstraintsUniqueEventTypeSeveralLoanWrittenOff;

public class ConstraintsUniqueEventTypeSeveralOneOpenLoan : IConstraint
{
    // A loan is open until it is returned or written off. Both end the cycle, so the
    // borrower can take the next loan whichever way the previous one finished.
    public void Define(IConstraintBuilder builder) =>
        builder
            .Unique<ConstraintsUniqueEventTypeSeveralLoanCheckedOut>()
            .RemovedWith<ConstraintsUniqueEventTypeSeveralLoanReturned>()
            .RemovedWith<ConstraintsUniqueEventTypeSeveralLoanWrittenOff>();
}
```
