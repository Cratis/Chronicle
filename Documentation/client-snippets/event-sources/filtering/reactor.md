```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record MoneyDeposited(decimal Amount);

[EventSource("Account")]
[EventStream("Onboarding")]
[EventStream("Transactions")]
public class AccountEventSource : IEventSource;

// Receives MoneyDeposited only when it was appended to the Transactions stream of the Account event source.
[FromEventSource<AccountEventSource>("Transactions")]
public class StatementReactor : IReactor
{
    public Task Deposited(MoneyDeposited @event, EventContext context) => Task.CompletedTask;
}
```
