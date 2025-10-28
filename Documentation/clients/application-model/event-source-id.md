# Event Source ID

Event Source ID is a fundamental concept in Chronicle that identifies the source of events. In the Application Model client, event source IDs are automatically resolved and managed to provide seamless integration within command handlers.

## How Event Source ID Resolution Works

The Application Model client uses the `EventSourceValuesProvider` to automatically determine the event source ID for commands. This process follows a specific resolution strategy:

### Resolution Strategy

1. **ICanProvideEventSourceId Interface**: If a command implements `ICanProvideEventSourceId`, it will use the provided event source ID
2. **Property Detection**: The system looks for properties of type `EventSourceId` or properties marked with `[Key]` attribute
3. **Tuple Support**: For tuple-based responses, it searches for `EventSourceId` values within the tuple
4. **Auto-generation**: If no event source ID is found, a new one is automatically generated

## ICanProvideEventSourceId Interface

Commands can explicitly provide their event source ID by implementing the `ICanProvideEventSourceId` interface:

```csharp
public record UpdateUserCommand(Guid UserId, string Name) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => UserId.ToString();
}
```

This approach gives you full control over how the event source ID is determined for your command.

## Property-Based Resolution

### EventSourceId Property

The simplest approach is to have a property of type `EventSourceId` in your command:

```csharp
public record CreateOrderCommand(EventSourceId OrderId, string CustomerName, decimal Amount);
```

### Key Attribute

You can also mark a property with the `[Key]` attribute to indicate it should be used as the event source ID:

```csharp
public record UpdateProductCommand([Key] Guid ProductId, string Name, decimal Price);
```

## Tuple-Based Resolution

When returning tuples from command handlers, the system will search for `EventSourceId` values within the tuple:

```csharp
public record CreateProductCommand(string Name, decimal Price)
{
    public (ProductCreated, EventSourceId) Handle()
    {
        var productId = EventSourceId.New();
        var productCreated = new ProductCreated
        {
            Name = Name,
            Price = Price
        };

        return (productCreated, productId);
    }
}
```

## Automatic Generation

If no event source ID can be resolved from the command, the system will automatically generate a new one using `EventSourceId.New()`. This is useful for creation scenarios where the ID is not known beforehand.

## EventSourceValuesProvider

The `EventSourceValuesProvider` is responsible for the resolution logic. It implements `ICommandContextValuesProvider` and is automatically registered with the dependency injection container.

### Implementation Details

The provider follows this logic:

1. Check if the command implements `ICanProvideEventSourceId`
2. If not, check if the command has an event source ID using `HasEventSourceId()` extension method
3. Extract the event source ID using `GetEventSourceId()` extension method
4. If none found, generate a new event source ID

## Command Context Integration

Once resolved, the event source ID is stored in the `CommandContext` under the well-known key `WellKnownCommandContextKeys.EventSourceId`. This makes it available throughout the command processing pipeline.

You can access the event source ID in your command handlers using:

```csharp
public record ProcessOrderCommand([Key] Guid OrderId)
{
    public object Handle(CommandContext commandContext)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        // Use the event source ID for processing
        return new OrderProcessed();
    }
}
```

## Error Handling

If an event source ID cannot be resolved when needed (for example, when accessing aggregate roots or read models), the following exceptions may be thrown:

- `MissingEventSourceIdInCommandContext`: When trying to access an event source ID that hasn't been set in the command context
- `UnableToResolveAggregateRootFromCommandContext`: When trying to resolve an aggregate root without a valid event source ID
- `UnableToResolveReadModelFromCommandContext`: When trying to resolve a read model without a valid event source ID

## Best Practices

1. **Be Explicit**: When possible, implement `ICanProvideEventSourceId` or use `EventSourceId` properties for clarity
2. **Consistent Naming**: Use consistent property names like `Id`, `EntityId`, or `[Entity]Id` for key properties
3. **Validation**: Ensure event source IDs are valid before processing commands
4. **Documentation**: Document which property serves as the event source ID in your commands

## Examples

### Creation Command

```csharp
public record CreateUserCommand(string Email, string Name);

// Event source ID will be auto-generated
```

### Update Command with Key

```csharp
public record UpdateUserCommand([Key] Guid UserId, string Name);
```

### Command with Explicit Event Source ID

```csharp
public record ActivateUserCommand(EventSourceId UserId);
```

### Custom Event Source ID Provider

```csharp
public record TransferFundsCommand(Guid FromAccountId, Guid ToAccountId, decimal Amount) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => FromAccountId.ToString();
}
```
