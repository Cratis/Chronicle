```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringInvoiceIssued(decimal Amount);

public record ReducersFilteringCustomerInvoiceTotal(decimal Amount);

public class ReducersFilteringInvoicingService(IEventLog eventLog)
{
    public Task IssueCustomerInvoice(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReducersFilteringInvoiceIssued(amount),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class ReducersFilteringCustomerInvoiceTotalReducer : IReducerFor<ReducersFilteringCustomerInvoiceTotal>
{
    public ReducersFilteringCustomerInvoiceTotal Issued(ReducersFilteringInvoiceIssued @event, ReducersFilteringCustomerInvoiceTotal? current, EventContext context) =>
        new((current?.Amount ?? 0m) + @event.Amount);
}
```
