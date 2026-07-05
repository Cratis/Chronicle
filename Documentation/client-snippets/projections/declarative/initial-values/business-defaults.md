```csharp title="Business defaults"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public enum InitialValuesOrderStatus
{
    Draft,
    Submitted
}

[EventType]
public record InitialValuesOrderSubmitted(string CustomerName, decimal TotalAmount);

public record InitialValuesOrderSummary(
    string CustomerName,
    InitialValuesOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt,
    string Notes);

public class InitialValuesOrderSummaryProjection : IProjectionFor<InitialValuesOrderSummary>
{
    public void Define(IProjectionBuilderFor<InitialValuesOrderSummary> builder) => builder
        .WithInitialValues(() => new InitialValuesOrderSummary(
            CustomerName: string.Empty,
            Status: InitialValuesOrderStatus.Draft,
            TotalAmount: 0m,
            SubmittedAt: DateTimeOffset.UnixEpoch,
            Notes: "No notes"))
        .From<InitialValuesOrderSubmitted>(_ => _
            .Set(m => m.Status).ToValue(InitialValuesOrderStatus.Submitted)
            .Set(m => m.SubmittedAt).ToEventContextProperty(c => c.Occurred));
}
```
