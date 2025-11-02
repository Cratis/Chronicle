# Event Source Type

Event Source Type is a metadata tag that provides an overarching, binding concept for events in Chronicle. It represents the broader entity or domain concept that events relate to, such as "Account", "Customer", or "Order".

## Overview

The Event Source Type helps organize and categorize events at a high level, making it easier to understand the domain context and track events across different streams and processes.

## Using EventSourceTypeAttribute

The simplest way to specify an event source type is by decorating your command with the `[EventSourceType]` attribute:

```csharp
[EventSourceType("Account")]
public record CreateAccountCommand(string AccountNumber, string Name);
```

This ensures that all events generated from this command will be tagged with the "Account" event source type.

## How It Works

The Application Model client uses the `EventSourceTypeValuesProvider` to automatically extract the event source type from commands decorated with the attribute. This value is then stored in the command context and passed along when appending events to the event log.

### Resolution Process

1. The `EventSourceTypeValuesProvider` checks if the command type has an `[EventSourceType]` attribute
2. If found, the value is extracted and added to the command context
3. When events are appended, the event source type is included in the event metadata

## Example Scenarios

### Account Management

```csharp
[EventSourceType("Account")]
public record OpenAccountCommand([Key] Guid AccountId, string AccountNumber)
{
    public AccountOpened Handle()
    {
        return new AccountOpened
        {
            AccountNumber = AccountNumber,
            OpenedAt = DateTime.UtcNow
        };
    }
}
```

### Customer Operations

```csharp
[EventSourceType("Customer")]
public record RegisterCustomerCommand(string Email, string Name)
{
    public CustomerRegistered Handle()
    {
        return new CustomerRegistered
        {
            Email = Email,
            Name = Name,
            RegisteredAt = DateTime.UtcNow
        };
    }
}
```

## Best Practices

1. **Use Domain Terms**: Choose event source type names that reflect your domain language
2. **Be Consistent**: Use the same event source type name across related commands
3. **Keep It High-Level**: Event source types should represent broad concepts, not specific processes
4. **Document Your Types**: Maintain a list of event source types used in your system

## Combining with Other Metadata

Event Source Type works in conjunction with other event metadata tags:

```csharp
[EventSourceType("Account")]
[EventStreamType("Onboarding")]
[EventStreamId("Monthly")]
public record CompleteAccountOnboardingCommand([Key] Guid AccountId)
{
    public AccountOnboardingCompleted Handle()
    {
        return new AccountOnboardingCompleted
        {
            CompletedAt = DateTime.UtcNow
        };
    }
}
```

## Integration with EventsCommandResponseValueHandler

When events are appended through the command pipeline, the `EventsCommandResponseValueHandler` automatically includes the event source type metadata:

```csharp
// The handler retrieves the event source type from the command context
var eventSourceType = commandContext.Values.TryGetValue(
    WellKnownCommandContextKeys.EventSourceType, 
    out var estValue) && estValue is EventSourceType est ? est : null;

// Events are appended with metadata
await eventLog.AppendMany(
    eventSourceId, 
    events, 
    eventStreamType, 
    eventStreamId, 
    eventSourceType);
```

## Related Documentation

- [Event Source ID](event-source-id.md) - Learn about event source identification
- [Event Stream Type](event-stream-type.md) - Understand event stream types
- [Event Stream ID](event-stream-id.md) - Learn about event stream identifiers
- [Event Metadata Tags](../../concepts/event-metadata-tags.md) - Complete overview of metadata tags
- [Commands](commands.md) - Command handling in Application Model
