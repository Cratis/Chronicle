# Reducers

Reducers provide a way to build read models by reducing a sequence of events into a single state. Unlike projections, which focus on transforming individual events into read models, reducers process collections of events together to compute derived state.

## Key Concepts

A reducer is a specialized observer that:

- **Processes multiple events together** - Events are grouped by event source and passed to the reducer as a collection
- **Computes derived state** - The reducer method receives the current state and returns the new state after processing the events
- **Maintains temporal consistency** - All events for a given event source are processed in order
- **Supports snapshots** - The computed state can be retrieved at any point in the event stream

## When to Use Reducers

Reducers are ideal when you need to:

- **Aggregate data across multiple events** - Calculate sums, averages, or other metrics from a series of events
- **Build temporal models** - Track how state changes over time
- **Implement complex business logic** - Process events together to derive insights that span multiple events
- **Create snapshots** - Capture the state of a read model at specific points in time

## Basic Example

```csharp
public record AccountBalance(decimal Balance, DateTimeOffset LastUpdated);

public class AccountBalanceReducer : IReducerFor<AccountBalance>
{
    public AccountBalance OnDepositMade(DepositMade @event, AccountBalance? current, EventContext context)
    {
        var currentBalance = current?.Balance ?? 0m;
        return new AccountBalance(currentBalance + @event.Amount, context.Occurred);
    }

    public AccountBalance OnWithdrawalMade(WithdrawalMade @event, AccountBalance? current, EventContext context)
    {
        var currentBalance = current?.Balance ?? 0m;
        return new AccountBalance(currentBalance - @event.Amount, context.Occurred);
    }
}
```

## Topics

- [Getting Started](getting-started.md) - Learn how to create your first reducer
- [Passive Reducers](passive-reducers.md) - Control when reducers actively observe events
- [Event Processing](event-processing.md) - Understand how reducers process events

## Reading Your Reducer-Based Read Models

Once you've defined a reducer, you can retrieve and observe the resulting read models using the `IReadModels` API:

- [Getting a Single Instance](../read-models/getting-single-instance.md) - Retrieve a specific instance by key with strong consistency
- [Getting a Collection of Instances](../read-models/getting-collection-instances.md) - Retrieve all instances for reporting and analysis
- [Getting Snapshots](../read-models/getting-snapshots.md) - Retrieve historical state snapshots grouped by correlation ID
- [Watching Read Models](../read-models/watching-read-models.md) - Observe real-time changes as events are applied
