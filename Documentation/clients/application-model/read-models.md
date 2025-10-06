# Read Models

Read Models in the Application Model client provide automatic dependency injection and seamless access to projections. The client automatically resolves read models based on the event source ID from the command context and the existence of corresponding projections.

## Overview

The Application Model client automatically discovers read models by examining projection types that implement `IProjectionFor<T>`. It then registers these read model types for dependency injection, allowing you to access current read model state within your command handlers.

## Automatic Registration

Read models are automatically discovered and registered when you configure the Application Model client.

The system will:

1. Scan for all projection types that implement `IProjectionFor<T>`
2. Extract the read model type `T` from the generic interface
3. Register the read model type as a transient service
4. Ensure the read model can be resolved using the current command context's event source ID

## Taking Dependencies on Read Models

You can inject read models directly into your commands through the Handle method signature:

```csharp
public record UpdateOrderCommand([Key] Guid OrderId, string Details)
{
    public object Handle(Order order, OrderSummary summary, ILogger<UpdateOrderCommand> logger)
    {
        // Use the read model to make decisions
        if (summary.TotalAmount > 1000m)
        {
            order.RequireApproval();
            return new OrderRequiresApproval();
        }
        else
        {
            order.Update(Details);
            return new OrderUpdated { Details = Details };
        }
    }
}
```

## Projection Requirements

For a read model to be automatically registered, you must have a corresponding projection:

```csharp
// The read model class
public class OrderSummary
{
    public EventSourceId OrderId { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public OrderStatus Status { get; set; }
}

// The projection that creates/updates the read model
public class OrderSummaryProjection : IProjectionFor<OrderSummary>
{
    public ProjectionId Identifier => "OrderSummary";

    public void Define(IProjectionBuilder<OrderSummary> builder)
    {
        builder
            .From<OrderCreated>()
            .Set(m => m.CustomerName).To(e => e.CustomerName)
            .Set(m => m.Status).To(OrderStatus.Created);

        builder
            .From<ItemAddedToOrder>()
            .Add(m => m.TotalAmount).With(e => e.Price * e.Quantity)
            .Increment(m => m.ItemCount);

        builder
            .From<OrderCancelled>()
            .Set(m => m.Status).To(OrderStatus.Cancelled);
    }
}
```

## Event Source ID Resolution

Read model resolution depends on the event source ID being available in the command context:

1. **Command Context Lookup**: The system retrieves the event source ID from the current `CommandContext`
2. **Validation**: If no event source ID is found, an `UnableToResolveReadModelFromCommandContext` exception is thrown
3. **Projection Query**: The `IProjections.GetInstanceById()` method is called with the read model type and event source ID
4. **Instance Return**: The current read model instance is returned

### Event Source ID Requirements

For read model resolution to work, the command must provide an event source ID through one of these methods:

- Implement `ICanProvideEventSourceId`
- Have a property of type `EventSourceId`
- Have a property marked with `[Key]` attribute
- Be part of a tuple that contains an `EventSourceId`

## Example Usage

### Basic Command Handler with Read Model

```csharp
public record UpdateInventoryCommand([Key] Guid ProductId, int QuantityChange)
{
    public InventoryUpdated Handle(Product product, ProductInventory inventory)
    {
        // Use read model to make business decisions
        if (inventory.CurrentStock + QuantityChange < 0)
        {
            throw new InsufficientStockException(ProductId);
        }

        if (inventory.CurrentStock + QuantityChange < inventory.ReorderLevel)
        {
            product.TriggerReorderAlert();
        }

        product.UpdateInventory(QuantityChange);

        return new InventoryUpdated
        {
            NewQuantity = inventory.CurrentStock + QuantityChange
        };
    }
}
```### Decision Making with Read Models

```csharp
public record ApproveOrderCommand(EventSourceId OrderId)
{
    public OrderApproved Handle(Order order, OrderSummary orderSummary, CustomerProfile customerProfile)
    {
        // Use multiple read models for complex business logic
        var requiresManualApproval =
            orderSummary.TotalAmount > 5000m ||
            customerProfile.RiskLevel == RiskLevel.High ||
            orderSummary.ItemCount > 10;

        if (requiresManualApproval)
        {
            order.RequireManualApproval();
        }
        else
        {
            order.AutoApprove();
        }

        return new OrderApproved
        {
            ApprovalType = requiresManualApproval ? "Manual" : "Automatic"
        };
    }
}
```## Error Handling

### UnableToResolveReadModelFromCommandContext

This exception is thrown when:

- No event source ID is available in the command context
- The event source ID is `EventSourceId.Unspecified`

```csharp
public record InvalidCommand(string SomeProperty);
// No event source ID property or interface implementation

// This will fail because no event source ID can be resolved
```

### Missing Projections

If you try to inject a read model that doesn't have a corresponding projection, the read model won't be registered and dependency injection will fail at runtime.

## Lifecycle Management

### Transient Scope

Read models are registered as transient services, meaning:

- A new instance is retrieved for each request
- The instance represents the current state at the time of the command
- The read model is immutable from the command handler's perspective

### Current State

Read models always represent the current projected state:

- They include all events that have been processed up to the point of resolution
- They do not include events that might be generated by the current command
- They reflect the state before any changes made by the current command handler

## Best Practices

### Read-Only Usage

Treat read models as read-only within command handlers:

```csharp
// Good: Using read model for decisions only
public record UpdateOrderCommand([Key] Guid OrderId)
{
    public object Handle(Order order, OrderSummary summary)
    {
        if (summary.Status == OrderStatus.Shipped)
        {
            throw new CannotUpdateShippedOrderException();
        }

        order.Update();  // Only modify the aggregate root
        return new OrderUpdated();
    }
}

// Bad: Don't try to modify read models directly
// Read models are immutable and changes won't be persisted
```### Efficient Projections

Design projections to include only the data needed for command decisions:

```csharp
// Good: Focused read model
public class OrderValidationModel
{
    public EventSourceId OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public bool RequiresApproval { get; set; }
}

// Less optimal: Kitchen sink approach
public class OrderEverything
{
    // Don't include every possible property if not needed
    public string CustomerAddress { get; set; }
    public List<OrderItem> Items { get; set; }
    public List<ShippingOption> AvailableShipping { get; set; }
    // ... many more properties
}
```

### Combining Multiple Read Models

When you need information from multiple contexts, consider injecting multiple focused read models:

```csharp
public record ProcessOrderCommand([Key] Guid OrderId)
{
    public object Handle(
        Order order,
        OrderSummary orderSummary,
        CustomerProfile customerProfile,
        InventoryStatus inventoryStatus)
    {
        // Use each read model for its specific concern
        ValidateCustomer(customerProfile);
        ValidateInventory(inventoryStatus);
        ApplyBusinessRules(orderSummary);

        order.Process();
        return new OrderProcessed();
    }

    private static void ValidateCustomer(CustomerProfile profile) { /* validation logic */ }
    private static void ValidateInventory(InventoryStatus status) { /* validation logic */ }
    private static void ApplyBusinessRules(OrderSummary summary) { /* business logic */ }
}
```## Advanced Scenarios

### Custom Read Model Resolution

If you need custom resolution logic, you can bypass automatic injection and use `IProjections` directly:

```csharp
public record SomeCommand(string SomeProperty)
{
    public object Handle(IProjections projections, CommandContext commandContext)
    {
        var customEventSourceId = DetermineCustomEventSourceId();
        var result = projections.GetInstanceById<MyReadModel>(customEventSourceId).GetAwaiter().GetResult();
        var readModel = result.ReadModel;

        // Process with custom-loaded read model
        return new SomeEvent();
    }

    private static EventSourceId DetermineCustomEventSourceId() => EventSourceId.New();
}
```### Conditional Read Model Usage

```csharp
public class ConditionalCommandHandler(CommandContext commandContext)
{
    public async Task Handle(ConditionalCommand command)
    {
        if (command.RequiresValidation)
        {
            // Only resolve read model when needed
            var serviceProvider = // Get from context or inject
            var validationModel = serviceProvider.GetRequiredService<ValidationReadModel>();

            if (!validationModel.IsValid)
            {
                throw new ValidationFailedException();
            }
        }
    }
}
```

### Aggregating Multiple Event Sources

For scenarios where you need to read from multiple event sources, consider creating dedicated query handlers or repositories:

```csharp
public record TransferOrderCommand([Key] Guid OrderId, Guid SourceOrderId, Guid TargetCustomerId)
{
    public object Handle(
        Order order,                    // Primary aggregate
        IOrderRepository orderRepo,     // For cross-aggregate queries
        ICustomerRepository customerRepo)
    {
        // Get read models for multiple event sources
        var sourceOrder = orderRepo.GetSummary(SourceOrderId).GetAwaiter().GetResult();
        var targetCustomer = customerRepo.GetProfile(TargetCustomerId).GetAwaiter().GetResult();

        // Use the primary aggregate for changes
        order.TransferTo(TargetCustomerId, sourceOrder.Items);
        return new OrderTransferred { TargetCustomerId = TargetCustomerId };
    }
}
```
