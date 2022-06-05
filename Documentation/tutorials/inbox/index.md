# Inbox

Once events are appended to the [outbox](../outbox/index.md), we can configure Cratis to connect one or more microservices together.
This is done by configuring the `cratis.json` file and **microservices** key.

> Note: Its assumed that Cratis is being used as a single cluster for multiple microservices. Support for cluster <-> cluster
> is coming in a future version.

```json
"microservices": {
    "0e1219ec-7136-40d8-a6e6-99c05ba22a30": { // Bank microservice identifier
        "name": "Bank"
    },
    "40dda9cf-38cc-4bf5-a249-ba3ce4f8861f": { // Balance notifier microservice identifier
        "name": "Balance Notifier",
        // We configure which outboxes the inbox will receive events from
        "inbox": {
            "fromOutboxes": [
                {
                    "microservice": "0e1219ec-7136-40d8-a6e6-99c05ba22a30"  // Bank
                }
            ]
        }
    }
}
```

## Observer

The system will now start appending events from the **Bank** microservices outbox automatically into the inbox of
**Balance notifier**.

Following the [Balance notifier sample](../../../Samples/BalanceNotifier/) we can imagine a separate service
that can notify when the balance changes.

Start by adding a file in the **Public** project (./Public/Accounts/Debit) called `AccountBalanceObserver.cs`.
Add the following to it.

```csharp
namespace Public.Accounts.Debit;

[Observer("292a21dc-71de-4042-a313-4bcd45f6e0cb", inbox: true)] // Inbox: true makes this observer observe the inbox instead of the event log.
public class AccountBalanceObserver
{
    // Regular event observer method
    public Task Balance(AccountBalance @event, EventContext context)
    {
        Console.WriteLine($"Balance for {context.EventSourceId} : {@event.Balance}");
        return Task.CompletedTask;
    }
}
```
