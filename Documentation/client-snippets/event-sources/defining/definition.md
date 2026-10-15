```csharp
using Cratis.Chronicle.EventSources;

[EventSource("Account", Description = "A customer's bank account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding", Description = "Opening the account and verifying the customer")]
[EventStream(
    "Transactions",
    Description = "Deposits and withdrawals, one stream per month",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class AccountEventSource : IEventSource;
```
