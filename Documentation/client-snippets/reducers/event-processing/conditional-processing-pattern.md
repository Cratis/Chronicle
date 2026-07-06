```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingAccountOpened(Guid AccountId);

[EventType]
public record EventProcessingDepositMade(decimal Amount);

[EventType]
public record EventProcessingAccountClosed;

public record EventProcessingAccount(Guid AccountId, decimal Balance, bool IsActive);

public class EventProcessingAccountReducer : IReducerFor<EventProcessingAccount>
{
    public EventProcessingAccount Opened(EventProcessingAccountOpened @event, EventProcessingAccount? current, EventContext context)
    {
        return new EventProcessingAccount(@event.AccountId, 0m, true);
    }

    public EventProcessingAccount? DepositMade(EventProcessingDepositMade @event, EventProcessingAccount? current, EventContext context)
    {
        // Skip if account doesn't exist or is not active
        if (current is null || !current.IsActive) return current;

        return current with { Balance = current.Balance + @event.Amount };
    }

    public EventProcessingAccount? Closed(EventProcessingAccountClosed @event, EventProcessingAccount? current, EventContext context)
    {
        if (current is null) return null;

        return current with { IsActive = false };
    }
}
```
