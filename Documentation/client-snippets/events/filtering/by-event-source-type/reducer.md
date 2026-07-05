```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record FilterBySourceTypeInvoiceIssued(decimal Amount);

public record FilterBySourceTypeCustomerInvoiceTotal(decimal Amount);

[EventSourceType("customer")]
public class FilterBySourceTypeCustomerInvoiceTotalReducer : IReducerFor<FilterBySourceTypeCustomerInvoiceTotal>
{
    public FilterBySourceTypeCustomerInvoiceTotal Issued(FilterBySourceTypeInvoiceIssued @event, FilterBySourceTypeCustomerInvoiceTotal? current, EventContext context) =>
        new((current?.Amount ?? 0m) + @event.Amount);
}

public class FilterBySourceTypeInvoicingService(IEventLog eventLog)
{
    public Task IssueCustomerInvoice(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterBySourceTypeInvoiceIssued(amount),
            eventSourceType: "customer");
}
```
