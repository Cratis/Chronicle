# Event Stream Type

Event Stream Type is a metadata tag that represents a concrete process or workflow related to an event source type. It helps organize events into logical streams based on the business process they belong to, such as "Onboarding", "Transactions", or "Compliance".

## Overview

In Chronicle, Event Stream Types are automatically linked to Aggregate Roots, making them a powerful tool for organizing domain logic around specific processes. This allows you to separate concerns and manage different aspects of an entity independently.

## Using EventStreamTypeAttribute

Decorate your command with the `[EventStreamType]` attribute to specify the stream type:

```csharp
[EventStreamType("Onboarding")]
public record StartOnboardingCommand([Key] Guid CustomerId, string Email);
```

All events generated from this command will be tagged with the "Onboarding" event stream type.

## How It Works

The Application Model client uses the `EventStreamTypeValuesProvider` to automatically extract the event stream type from commands decorated with the attribute. This value is then stored in the command context and passed along when appending events to the event log.

### Resolution Process

1. The `EventStreamTypeValuesProvider` checks if the command type has an `[EventStreamType]` attribute
2. If found, the value is extracted and added to the command context
3. When events are appended, the event stream type is included in the event metadata

## Example Scenarios

### Account Onboarding Process

```csharp
[EventSourceType("Account")]
[EventStreamType("Onboarding")]
public record InitiateAccountOnboardingCommand([Key] Guid AccountId, string AccountNumber)
{
    public AccountOnboardingInitiated Handle()
    {
        return new AccountOnboardingInitiated
        {
            AccountNumber = AccountNumber,
            InitiatedAt = DateTime.UtcNow
        };
    }
}

[EventSourceType("Account")]
[EventStreamType("Onboarding")]
public record CompleteKYCVerificationCommand([Key] Guid AccountId)
{
    public KYCVerificationCompleted Handle()
    {
        return new KYCVerificationCompleted
        {
            VerifiedAt = DateTime.UtcNow
        };
    }
}
```

### Transaction Processing

```csharp
[EventSourceType("Account")]
[EventStreamType("Transactions")]
public record ProcessPaymentCommand([Key] Guid AccountId, decimal Amount, string Reference)
{
    public PaymentProcessed Handle()
    {
        return new PaymentProcessed
        {
            Amount = Amount,
            Reference = Reference,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

### Compliance Workflows

```csharp
[EventSourceType("Account")]
[EventStreamType("Compliance")]
public record FlagSuspiciousActivityCommand([Key] Guid AccountId, string Reason)
{
    public SuspiciousActivityFlagged Handle()
    {
        return new SuspiciousActivityFlagged
        {
            Reason = Reason,
            FlaggedAt = DateTime.UtcNow
        };
    }
}
```

## Best Practices

1. **Process-Oriented**: Name event stream types after business processes, not technical concepts
2. **Bounded Context Alignment**: Align stream types with your domain's bounded contexts
3. **Aggregate Root Mapping**: Consider how stream types map to aggregate roots in your domain model
4. **Consistency**: Use the same stream type name across related commands within a process

## Relationship with Aggregate Roots

In Chronicle, Event Stream Types are automatically linked to Aggregate Roots. This means that when you define a stream type, you're implicitly defining a process that can be modeled as an aggregate root:

```csharp
// The event stream type "Onboarding" can be modeled as an aggregate root
public class AccountOnboarding : AggregateRoot
{
    public void Initiate(string accountNumber)
    {
        Apply(new AccountOnboardingInitiated { AccountNumber = accountNumber });
    }

    public void CompleteKYC()
    {
        Apply(new KYCVerificationCompleted { VerifiedAt = DateTime.UtcNow });
    }
}
```

## Combining with Other Metadata

Event Stream Type works seamlessly with other event metadata tags:

```csharp
[EventSourceType("Account")]
[EventStreamType("Onboarding")]
[EventStreamId("Monthly")]
public record ProcessMonthlyOnboardingCommand([Key] Guid AccountId)
{
    public MonthlyOnboardingProcessed Handle()
    {
        return new MonthlyOnboardingProcessed
        {
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

## Integration with EventsCommandResponseValueHandler

When events are appended through the command pipeline, the `EventsCommandResponseValueHandler` automatically includes the event stream type metadata:

```csharp
// The handler retrieves the event stream type from the command context
var eventStreamType = commandContext.Values.TryGetValue(
    WellKnownCommandContextKeys.EventStreamType, 
    out var estrValue) && estrValue is EventStreamType estr ? estr : null;

// Events are appended with metadata
await eventLog.AppendMany(
    eventSourceId, 
    events, 
    eventStreamType, 
    eventStreamId, 
    eventSourceType);
```

## Default Behavior

If no event stream type is specified, Chronicle uses the default stream type "All". This is useful for events that don't belong to a specific process.

## Related Documentation

- [Event Source ID](event-source-id.md) - Learn about event source identification
- [Event Source Type](event-source-type.md) - Understand event source types
- [Event Stream ID](event-stream-id.md) - Learn about event stream identifiers
- [Event Metadata Tags](../../concepts/event-metadata-tags.md) - Complete overview of metadata tags
- [Aggregate Roots](aggregate-roots.md) - Working with aggregate roots
- [Commands](commands.md) - Command handling in Application Model
