# Event Stream ID

Event Stream ID is a metadata tag that provides a marker to separate independent streams within a stream type. It allows you to create logical partitions for events, such as "Monthly", "Yearly", "RegionA", or "TenantX".

For a complete overview of event metadata tags in Chronicle, see [Event Metadata Tags](../../../concepts/event-metadata-tags.md).

## Overview

Event Stream IDs enable you to organize events into separate logical streams while keeping them within the same event stream type. This is particularly useful for multi-tenant scenarios, time-based partitioning, or geographical segmentation.

## Two Ways to Provide Event Stream ID

Chronicle offers two approaches to specify event stream IDs: using an attribute or implementing an interface.

### Using EventStreamIdAttribute

The simplest approach is to use the `[EventStreamId]` attribute with a static value:

```csharp
[EventStreamId("Monthly")]
public record ProcessMonthlyReportCommand([Key] Guid AccountId);
```

### Using ICanProvideEventStreamId Interface

For dynamic event stream IDs, implement the `ICanProvideEventStreamId` interface:

```csharp
public record ProcessTenantOperationCommand(Guid TenantId, [Key] Guid EntityId) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => TenantId.ToString();

    public TenantOperationProcessed Handle()
    {
        return new TenantOperationProcessed
        {
            TenantId = TenantId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

### Combining Attribute and Interface

You can also combine both approaches when you want to use the interface for dynamic values while enabling concurrency control (see [Concurrency Scope](#concurrency-scope) below):

```csharp
[EventStreamId(concurrency: true)]
public record ProcessTenantOperationCommand(Guid TenantId, [Key] Guid EntityId) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => TenantId.ToString();

    public TenantOperationProcessed Handle()
    {
        return new TenantOperationProcessed
        {
            TenantId = TenantId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

In this case, the attribute without a value serves to enable concurrency scope inclusion, while the interface provides the actual runtime value.

## Ambiguity Check

**Important**: You cannot provide the event stream ID value in both the attribute AND the interface. Using both with values will throw an `AmbiguousEventStreamId` exception:

```csharp
// ❌ This will throw AmbiguousEventStreamId exception
[EventStreamId("Static")]
public record InvalidCommand(Guid DynamicId) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => DynamicId.ToString();
}
```

The error message will indicate:
> Command 'InvalidCommand' has both EventStreamIdAttribute with a value and implements ICanProvideEventStreamId. Please use only one method to provide the event stream id, or set the attribute value to null to use the interface.

## Concurrency Scope

Event metadata attributes support a `concurrency` parameter that controls whether the metadata should be included in the concurrency scope when appending events. This provides fine-grained control over optimistic concurrency validation.

### Basic Usage

Add `concurrency: true` to include the event stream ID in the concurrency scope:

```csharp
[EventStreamId("Monthly", concurrency: true)]
public record GenerateMonthlyReportCommand([Key] Guid ReportId)
{
    public MonthlyReportGenerated Handle()
    {
        return new MonthlyReportGenerated
        {
            GeneratedAt = DateTime.UtcNow
        };
    }
}
```

When `concurrency: true` is specified, the event stream ID becomes part of the concurrency scope, ensuring that concurrent append operations on the same stream ID are properly validated.

### Combining Multiple Metadata in Concurrency Scope

You can include multiple metadata attributes in the concurrency scope:

```csharp
[EventSourceType("Order", concurrency: true)]
[EventStreamType("Processing", concurrency: true)]
[EventStreamId("Priority", concurrency: true)]
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

This creates a concurrency scope that includes all three metadata values, providing stricter validation.

### Dynamic Values with Concurrency

When using the interface for dynamic values, you can still enable concurrency by using the attribute without a value:

```csharp
[EventStreamId(concurrency: true)]
public record ProcessTenantOrderCommand(Guid TenantId, [Key] Guid OrderId) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => TenantId.ToString();

    public TenantOrderProcessed Handle()
    {
        return new TenantOrderProcessed
        {
            TenantId = TenantId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

The attribute enables concurrency scope inclusion while the interface provides the runtime value.

### When to Use Concurrency Scope

Use concurrency scope inclusion when:

- You need to prevent concurrent modifications to the same logical stream
- You want to ensure events are appended in a consistent order
- You're implementing optimistic concurrency control at the stream level
- You have multiple processes that might append events to the same stream simultaneously

**Note**: By default, `concurrency` is `false`, meaning metadata is not included in the concurrency scope. Only enable it when you specifically need concurrency validation.

## How It Works

The Application Model client uses the `EventStreamIdValuesProvider` to automatically extract the event stream ID. The provider follows this logic:

1. Check if the command has the `[EventStreamId]` attribute
2. Check if the command implements `ICanProvideEventStreamId`
3. If both are present with values, throw `AmbiguousEventStreamId` exception
4. If interface is implemented, call `GetEventStreamId()` method
5. If attribute is present with a value, use the attribute value
6. If neither provides a value, no event stream ID is added to the context

When events are appended, the `ConcurrencyScopeBuilder` checks which metadata attributes have `concurrency: true` and builds a concurrency scope accordingly. If no metadata has concurrency enabled, no concurrency scope is created.

## Example Scenarios

### Time-Based Partitioning

```csharp
[EventSourceType("Sales")]
[EventStreamType("Reports")]
[EventStreamId("Monthly")]
public record GenerateMonthlyReportCommand([Key] Guid ReportId, int Year, int Month)
{
    public MonthlyReportGenerated Handle()
    {
        return new MonthlyReportGenerated
        {
            Year = Year,
            Month = Month,
            GeneratedAt = DateTime.UtcNow
        };
    }
}

[EventSourceType("Sales")]
[EventStreamType("Reports")]
[EventStreamId("Yearly")]
public record GenerateYearlyReportCommand([Key] Guid ReportId, int Year)
{
    public YearlyReportGenerated Handle()
    {
        return new YearlyReportGenerated
        {
            Year = Year,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
```

### Multi-Tenant Scenarios

```csharp
[EventSourceType("Order")]
[EventStreamType("Processing")]
public record ProcessOrderCommand(Guid TenantId, [Key] Guid OrderId, List<OrderItem> Items)
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => TenantId.ToString();

    public OrderProcessed Handle()
    {
        return new OrderProcessed
        {
            TenantId = TenantId,
            OrderId = OrderId,
            Items = Items,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

### Regional Segmentation

```csharp
public record ProcessRegionalTransactionCommand(string Region, [Key] Guid TransactionId, decimal Amount)
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => Region;

    public RegionalTransactionProcessed Handle()
    {
        return new RegionalTransactionProcessed
        {
            Region = Region,
            TransactionId = TransactionId,
            Amount = Amount,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

### Dynamic Categorization

```csharp
public record CategorizeCustomerActivityCommand(
    [Key] Guid CustomerId,
    CustomerTier Tier) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => Tier switch
    {
        CustomerTier.Premium => "Premium",
        CustomerTier.Standard => "Standard",
        CustomerTier.Basic => "Basic",
        _ => "Uncategorized"
    };

    public CustomerActivityCategorized Handle()
    {
        return new CustomerActivityCategorized
        {
            CustomerId = CustomerId,
            Tier = Tier,
            CategorizedAt = DateTime.UtcNow
        };
    }
}
```

## Best Practices

1. **Choose the Right Approach**:
   - Use `[EventStreamId]` for static, compile-time known stream IDs
   - Use `ICanProvideEventStreamId` for dynamic, runtime-determined stream IDs

2. **Meaningful Identifiers**: Use descriptive stream ID values that clearly indicate the partition

3. **Consistency**: Maintain consistent stream ID naming across your application

4. **Avoid Over-Partitioning**: Don't create too many stream IDs; find the right balance for your use case

5. **Document Stream IDs**: Maintain documentation of the stream IDs used in your system

## Combining with Other Metadata

Event Stream ID works in conjunction with other event metadata tags:

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

When events are appended through the command pipeline, the `EventsCommandResponseValueHandler` automatically includes the event stream ID metadata and builds a concurrency scope if needed:

```csharp
// The handler retrieves the event stream ID from the command context
var eventStreamId = commandContext.Values.TryGetValue(
    WellKnownCommandContextKeys.EventStreamId,
    out var esiValue) && esiValue is EventStreamId esi ? esi : null;

// Build concurrency scope based on metadata attributes with concurrency: true
var concurrencyScope = ConcurrencyScopeBuilder.BuildFromCommandContext(commandContext);

// Events are appended with metadata and optional concurrency scope
await eventLog.AppendMany(
    eventSourceId,
    events,
    eventStreamType,
    eventStreamId,
    eventSourceType,
    correlationId: default,
    concurrencyScope: concurrencyScope);
```

The `SingleEventCommandResponseValueHandler` works the same way for single event commands.

## Default Behavior

If no event stream ID is specified, Chronicle uses the default stream ID "Default". This is suitable for scenarios where stream partitioning is not needed.

## Related Documentation

- [Event Source ID](event-source-id.md) - Learn about event source identification
- [Event Source Type](event-source-type.md) - Understand event source types
- [Event Stream Type](event-stream-type.md) - Learn about event stream types
- [Event Metadata Tags](../../../concepts/event-metadata-tags.md) - Complete overview of metadata tags
- [Concurrency Control](../../dotnet/events/concurrency.md) - Learn about concurrency scopes in Chronicle
- [Commands](../commands.md) - Command handling in Application Model
