# Filter reducers and reactors by event source type

Use `[EventSourceType]` on a reducer or reactor when it should only handle events appended with a specific event source type.

## How the match works

Chronicle compares the observer attribute to the `eventSourceType:` value used when appending the event. If the values do not match, the reducer or reactor is skipped.

## Filter a reactor by event source type

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record CustomerRegistered(string EmailAddress);

public class CustomerRegistrationService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new CustomerRegistered(emailAddress),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class CustomerWelcomeReactor : IReactor
{
    public Task Registered(CustomerRegistered @event, EventContext context) => Task.CompletedTask;
}
```

The reactor is only invoked for events appended with `eventSourceType: "customer"`.

## Filter a reducer by event source type

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record InvoiceIssued(decimal Amount);

public record CustomerInvoiceTotal(decimal Amount);

[EventSourceType("customer")]
public class CustomerInvoiceTotalReducer : IReducerFor<CustomerInvoiceTotal>
{
    public CustomerInvoiceTotal Issued(InvoiceIssued @event, CustomerInvoiceTotal? current, EventContext context) =>
        new((current?.Amount ?? 0m) + @event.Amount);
}

public class InvoicingService(IEventLog eventLog)
{
    public Task IssueCustomerInvoice(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new InvoiceIssued(amount),
            eventSourceType: "customer");
}
```

If you append the same event with `eventSourceType: "supplier"`, this reducer does not receive it.

## Combine with other filters

You can combine `[EventSourceType]` with `[FilterEventsByTag]` or `[EventStreamType]`. When you do, Chronicle requires all configured filter categories to match.
