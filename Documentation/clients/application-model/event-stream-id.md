# Event Stream ID

Event Stream ID is a metadata tag that provides a marker to separate independent streams within a stream type. It allows you to create logical partitions for events, such as "Monthly", "Yearly", "RegionA", or "TenantX".

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

## Ambiguity Check

**Important**: You must use either the attribute OR the interface, but not both. Using both will throw an `AmbiguousEventStreamId` exception:

```csharp
// ❌ This will throw AmbiguousEventStreamId exception
[EventStreamId("Static")]
public record InvalidCommand(Guid DynamicId) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => DynamicId.ToString();
}
```

The error message will indicate:
> Command 'InvalidCommand' has both EventStreamIdAttribute and implements ICanProvideEventStreamId. Please use only one method to provide the event stream id.

## How It Works

The Application Model client uses the `EventStreamIdValuesProvider` to automatically extract the event stream ID. The provider follows this logic:

1. Check if the command has the `[EventStreamId]` attribute
2. Check if the command implements `ICanProvideEventStreamId`
3. If both are present, throw `AmbiguousEventStreamId` exception
4. If interface is implemented, call `GetEventStreamId()` method
5. If attribute is present, use the attribute value
6. If neither is present, no event stream ID is added to the context

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

When events are appended through the command pipeline, the `EventsCommandResponseValueHandler` automatically includes the event stream ID metadata:

```csharp
// The handler retrieves the event stream ID from the command context
var eventStreamId = commandContext.Values.TryGetValue(
    WellKnownCommandContextKeys.EventStreamId, 
    out var esiValue) && esiValue is EventStreamId esi ? esi : null;

// Events are appended with metadata
await eventLog.AppendMany(
    eventSourceId, 
    events, 
    eventStreamType, 
    eventStreamId, 
    eventSourceType);
```

## Default Behavior

If no event stream ID is specified, Chronicle uses the default stream ID "Default". This is suitable for scenarios where stream partitioning is not needed.

## Related Documentation

- [Event Source ID](event-source-id.md) - Learn about event source identification
- [Event Source Type](event-source-type.md) - Understand event source types
- [Event Stream Type](event-stream-type.md) - Learn about event stream types
- [Event Metadata Tags](../../concepts/event-metadata-tags.md) - Complete overview of metadata tags
- [Commands](commands.md) - Command handling in Application Model
