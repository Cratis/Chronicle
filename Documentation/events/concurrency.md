---
uid: Chronicle.ConcurrencyScope
---
# Concurrency

Concurrency control in Chronicle ensures that multiple operations don't interfere with each other when appending events to the same event source. Chronicle provides a sophisticated concurrency control mechanism through the `ConcurrencyScope` concept, which allows you to define precisely how concurrency should be handled based on event metadata tags.

## Understanding ConcurrencyScope

A `ConcurrencyScope` defines the boundaries and constraints for concurrent operations when appending events. It uses the [event metadata tags](../concepts/event-metadata-tags.md) to scope concurrency control to specific aspects of your events, providing fine-grained control over when concurrency violations should be detected.

### Event Metadata Tags for Concurrency

Chronicle uses the following event metadata tags to scope concurrency:

- **EventSourceId**: Unique identifier for the event source
- **EventSourceType**: Overarching, binding concept (e.g., Account)
- **EventStreamType**: A concrete process related to event source type (e.g., Onboarding, Transactions)
- **EventStreamId**: A marker to separate independent streams for a stream type (e.g., Monthly, Yearly)
- **EventTypes**: Specific event types to scope concurrency to

## Basic Usage

### Simple Concurrency Scope

The most basic form of concurrency control scopes to a specific sequence number for an event source:

```csharp
using Cratis.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

public class BankAccountService(IEventLog eventLog)
{
    public async Task OpenAccount(AccountId accountId, string accountName)
    {
        var concurrencyScope = new ConcurrencyScope(
            sequenceNumber: 42,  // Expected sequence number
            eventSourceId: accountId
        );

        await eventLog.Append(
            accountId,
            new AccountOpened(accountName),
            concurrencyScope: concurrencyScope
        );
    }
}
```

### Using ConcurrencyScopeBuilder

For more complex scenarios, use the `ConcurrencyScopeBuilder` to fluently construct concurrency scopes:

```csharp
public class AccountTransactionService(IEventLog eventLog)
{
    public async Task ProcessTransaction(AccountId accountId, decimal amount)
    {
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithSequenceNumber(15)
            .WithEventStreamType("Transactions")
            .WithEventType<MoneyDeposited>()
            .WithEventType<MoneyWithdrawn>()
            .Build();

        await eventLog.Append(
            accountId,
            new MoneyDeposited(amount),
            concurrencyScope: concurrencyScope
        );
    }
}
```

## Scoping by Event Metadata Tags

### EventSourceType and EventStreamType

You can scope concurrency to specific event source types and stream types:

```csharp
public class AccountManagementService(IEventLog eventLog)
{
    public async Task UpdateAccountSettings(AccountId accountId, AccountSettings settings)
    {
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithEventSourceType("BankAccount")
            .WithEventStreamType("AccountManagement")
            .WithSequenceNumber(10)
            .Build();

        await eventLog.Append(
            accountId,
            new AccountSettingsUpdated(settings),
            eventSourceType: "BankAccount",
            eventStreamType: "AccountManagement",
            concurrencyScope: concurrencyScope
        );
    }
}
```

### EventStreamId

Use event stream IDs to scope concurrency to specific streams within a stream type:

```csharp
public class MonthlyReportService(IEventLog eventLog)
{
    public async Task GenerateMonthlyReport(AccountId accountId, DateTime month)
    {
        var monthKey = month.ToString("yyyy-MM");

        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithEventStreamType("Reporting")
            .WithEventStreamId(monthKey)
            .WithSequenceNumber(5)
            .Build();

        await eventLog.Append(
            accountId,
            new MonthlyReportGenerated(month),
            eventStreamType: "Reporting",
            eventStreamId: monthKey,
            concurrencyScope: concurrencyScope
        );
    }
}
```

### Event Types

Scope concurrency to specific event types to allow concurrent operations on different types of events:

```csharp
public class AccountService(IEventLog eventLog)
{
    public async Task ProcessPayment(AccountId accountId, decimal amount)
    {
        // Only check concurrency for payment-related events
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithSequenceNumber(20)
            .WithEventType<PaymentProcessed>()
            .WithEventType<PaymentFailed>()
            .WithEventType<PaymentRefunded>()
            .Build();

        await eventLog.Append(
            accountId,
            new PaymentProcessed(amount),
            concurrencyScope: concurrencyScope
        );
    }
}
```

## AppendMany with Concurrency Scopes

When appending multiple events, you can specify different concurrency scopes for different event sources:

```csharp
public class TransferService(IEventLog eventLog)
{
    public async Task TransferMoney(AccountId fromAccount, AccountId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new MoneyWithdrawn(amount)),
            new EventForEventSourceId(toAccount, new MoneyDeposited(amount))
        };

        var concurrencyScopes = new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [fromAccount] = new ConcurrencyScopeBuilder()
                .WithEventSourceId(fromAccount)
                .WithSequenceNumber(50)
                .WithEventType<MoneyWithdrawn>()
                .Build(),

            [toAccount] = new ConcurrencyScopeBuilder()
                .WithEventSourceId(toAccount)
                .WithSequenceNumber(25)
                .WithEventType<MoneyDeposited>()
                .Build()
        };

        await eventLog.AppendMany(events, concurrencyScopes: concurrencyScopes);
    }
}
```

## Event Source Operations with Concurrency

You can also use concurrency scopes with event source operations:

```csharp
public class BatchAccountProcessor(IEventLog eventLog)
{
    public async Task ProcessAccountBatch(AccountId accountId)
    {
        await eventLog
            .For(accountId)
            .WithConcurrencyScope(scope => scope
                .WithSequenceNumber(30)
                .WithEventType<AccountProcessed>()
                .WithEventType<AccountValidated>())
            .Append(new AccountValidated())
            .Append(new AccountProcessed())
            .Commit();
    }
}
```

## Handling Concurrency Violations

When a concurrency violation occurs, Chronicle will return a `ConcurrencyViolation` in the append result:

```csharp
public class SafeAccountService(IEventLog eventLog)
{
    public async Task<bool> TryOpenAccount(AccountId accountId, string accountName)
    {
        var concurrencyScope = new ConcurrencyScope(
            sequenceNumber: 0,  // Expect this to be the first event
            eventSourceId: accountId
        );

        var result = await eventLog.Append(
            accountId,
            new AccountOpened(accountName),
            concurrencyScope: concurrencyScope
        );

        if (!result.IsSuccess)
        {
            // Handle the violation - maybe retry or return false
            return false;
        }

        return result.IsSuccess;
    }
}
```

## Concurrency Strategies

Chronicle provides built-in concurrency strategies:

### Optimistic Concurrency Strategy

This strategy gets the current tail sequence number and uses it as the expected sequence number:

```csharp
// Configured automatically when using dependency injection
public class OptimisticAccountService(IEventLog eventLog, IConcurrencyScopeStrategies strategies)
{
    public async Task UpdateAccount(AccountId accountId, string newName)
    {
        var strategy = strategies.GetFor<Account>();
        var concurrencyScope = await strategy.GetScope(accountId);

        await eventLog.Append(
            accountId,
            new AccountNameChanged(newName),
            concurrencyScope: concurrencyScope
        );
    }
}
```

> **Note**: When using aggregate roots, the `EventStreamType` is automatically set based on the aggregate root's type name or the `[EventStreamType]` attribute if specified. Aggregate roots automatically scope concurrency to their specific event stream type, ensuring that concurrent operations on different stream types don't interfere with each other.

## Best Practices

1. **Use specific scoping**: Scope concurrency as narrowly as possible to avoid unnecessary blocking
2. **Event type scoping**: When possible, scope to specific event types to allow concurrent operations on different event types
3. **Handle violations gracefully**: Always check for concurrency violations and implement appropriate retry or fallback logic
4. **Use builders for complex scopes**: The `ConcurrencyScopeBuilder` provides a clear, fluent API for complex concurrency requirements
5. **Aggregate root integration**: Leverage aggregate roots for automatic stream type management and built-in concurrency strategies
