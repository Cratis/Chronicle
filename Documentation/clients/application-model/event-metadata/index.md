# Event Metadata

Event metadata provides a structured way to categorize and organize events within Chronicle. These metadata tags help you track the context and purpose of events, making it easier to query, filter, and process them in your application.

For a complete overview of event metadata tags in Chronicle, see [Event Metadata Tags](../../../concepts/event-metadata-tags.md).

## Overview

Chronicle supports several types of event metadata that work together to provide rich context for your events:

- **Event Source ID**: Uniquely identifies the source of events
- **Event Source Type**: Represents the overarching entity or domain concept
- **Event Stream Type**: Identifies the specific process or workflow
- **Event Stream ID**: Provides a marker for separating independent streams

## Event Source ID

The Event Source ID uniquely identifies the entity that events relate to. In the Application Model client, event source IDs are automatically resolved from commands using various strategies including interfaces, properties, and attributes.

[Read more about Event Source ID →](event-source-id.md)

## Event Source Type

Event Source Type provides an overarching, binding concept for events. It represents the broader entity or domain concept that events relate to, such as "Account", "Customer", or "Order". Use the `[EventSourceType]` attribute on your commands to specify this metadata.

[Read more about Event Source Type →](event-source-type.md)

## Event Stream Type

Event Stream Type represents a concrete process or workflow related to an event source type. It helps organize events into logical streams based on the business process they belong to, such as "Onboarding", "Transactions", or "Compliance". Event Stream Types are automatically linked to Aggregate Roots in Chronicle.

[Read more about Event Stream Type →](event-stream-type.md)

## Event Stream ID

Event Stream ID provides a marker to separate independent streams within a stream type. It allows you to create logical partitions for events, such as "Monthly", "Yearly", "RegionA", or "TenantX". You can provide event stream IDs using either the `[EventStreamId]` attribute or by implementing the `ICanProvideEventStreamId` interface. Event metadata attributes also support a `concurrency` parameter for fine-grained concurrency control.

[Read more about Event Stream ID →](event-stream-id.md)

## Using Event Metadata in Commands

Event metadata is typically specified using attributes on your command records:

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

## Concurrency Control with Event Metadata

Event metadata attributes support a `concurrency` parameter that enables fine-grained optimistic concurrency control. When set to `true`, the metadata value is included in the concurrency scope during event appending:

```csharp
[EventStreamId("Priority", concurrency: true)]
[EventStreamType("OrderProcessing", concurrency: true)]
public record ProcessPriorityOrderCommand([Key] Guid OrderId)
{
    public PriorityOrderProcessed Handle()
    {
        return new PriorityOrderProcessed
        {
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

This ensures that concurrent append operations on the same stream are properly validated, preventing race conditions. By default, concurrency is `false`, meaning metadata is used only for categorization and not for concurrency validation.

[Learn more about concurrency scopes in Event Stream ID documentation →](event-stream-id.md#concurrency-scope)

## Best Practices

1. **Use Domain Language**: Choose metadata values that reflect your domain terminology
2. **Be Consistent**: Use the same metadata values across related commands
3. **Document Your Choices**: Maintain documentation of the metadata values used in your system
4. **Consider Querying**: Think about how you'll query and filter events when choosing metadata values
5. **Keep It Simple**: Don't over-complicate your metadata structure

## Related Documentation

- [Event Metadata Tags](../../../concepts/event-metadata-tags.md) - Complete overview of metadata tags
- [Commands](../commands.md) - Command handling in Application Model
- [Aggregate Roots](../aggregate-roots.md) - Working with aggregate roots
