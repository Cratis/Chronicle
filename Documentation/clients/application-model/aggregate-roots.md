# Aggregate Roots

Aggregate Roots in the Application Model client provide automatic dependency injection and seamless integration with Chronicle's event sourcing capabilities. The client automatically resolves aggregate roots based on the event source ID from the command context.

## Overview

The Application Model client automatically registers all aggregate root types and provides them as transient services in the dependency injection container. When an aggregate root is requested, it uses the event source ID from the current command context to load the appropriate instance.

## Automatic Registration

Aggregate roots are automatically discovered and registered when you configure the Application Model client. See [getting started](./getting-started.md) for more details.

This will scan for all aggregate root types and register them with the dependency injection container.

## Taking Dependencies on Aggregate Roots

You can inject aggregate roots directly into your commands through the Handle method signature:

```csharp
public record AddItemToOrderCommand([Key] Guid OrderId, Guid ProductId, int Quantity, decimal Price)
{
    public object Handle(Order order, ILogger<AddItemToOrderCommand> logger)
    {
        order.AddItem(ProductId, Quantity, Price);

        // The changes are automatically tracked and will be committed
        // when the command handler completes successfully
        return new ItemAddedToOrder { ProductId = ProductId, Quantity = Quantity, Price = Price };
    }
}
```

## Event Source ID Resolution

The aggregate root resolution depends entirely on the event source ID being available in the command context. The resolution process works as follows:

1. **Command Context Lookup**: The system retrieves the event source ID from the current `CommandContext`
2. **Validation**: If no event source ID is found, an `UnableToResolveAggregateRootFromCommandContext` exception is thrown
3. **Factory Invocation**: The `IAggregateRootFactory.Get<T>()` method is called with the resolved event source ID
4. **Instance Return**: The loaded aggregate root instance is returned

### Event Source ID Requirements

For aggregate root resolution to work, the command must provide an event source ID through one of these methods:

- Implement `ICanProvideEventSourceId`
- Have a property of type `EventSourceId`
- Have a property marked with `[Key]` attribute
- Be part of a tuple that contains an `EventSourceId`

## Example Usage

### Basic Command Handler

```csharp
public record CreateUserCommand(EventSourceId UserId, string Email, string Name)
{
    public UserCreated Handle(User user)
    {
        // The 'user' aggregate root is automatically loaded using the UserId
        // from the command as the event source ID

        user.Create(Email, Name);

        return new UserCreated
        {
            Email = Email,
            Name = Name
        };
    }
}
```

### Update Command Handler

```csharp
public record UpdateUserEmailCommand([Key] Guid UserId, string NewEmail)
{
    public UserEmailUpdated Handle(User user)
    {
        // The 'user' aggregate root is loaded using UserId as event source ID
        user.UpdateEmail(NewEmail);

        return new UserEmailUpdated { NewEmail = NewEmail };
    }
}
```

## Multiple Aggregate Roots

Note that with the current pattern, you can only automatically resolve one aggregate root per command (based on the event source ID). For scenarios involving multiple aggregates, you'll need to load additional ones manually:

```csharp
public record TransferFundsCommand(Guid FromAccountId, Guid ToAccountId, decimal Amount) : ICanProvideEventSourceId
{
    // This command uses FromAccountId as the primary event source
    public EventSourceId GetEventSourceId() => FromAccountId.ToString();

    public FundsTransferred Handle(Account fromAccount, IAccountRepository accountRepository)
    {
        // Load the target account manually since we can only auto-resolve one
        var toAccount = accountRepository.GetById(ToAccountId).GetAwaiter().GetResult();

        fromAccount.TransferTo(toAccount, Amount);

        return new FundsTransferred
        {
            ToAccountId = ToAccountId,
            Amount = Amount
        };
    }
}
```## Error Handling

### UnableToResolveAggregateRootFromCommandContext

This exception is thrown when:

- No event source ID is available in the command context
- The event source ID is `EventSourceId.Unspecified`

```csharp
public record InvalidCommand(string SomeProperty);
// No event source ID property or interface implementation

// This will fail because no event source ID can be resolved
```

## Lifecycle Management

### Transient Scope

Aggregate roots are registered as transient services, meaning:

- A new instance is created for each request
- The instance is tied to the specific event source ID from the command context
- Changes made to the aggregate root are automatically tracked
- The aggregate root is automatically disposed after the command completes

### Automatic Commit

When using aggregate roots through dependency injection:

- Changes are automatically tracked by Chronicle's change tracking system
- Events generated by the aggregate root are automatically committed when the command handler completes successfully
- If an exception occurs, changes are automatically rolled back

## Best Practices

### Single Responsibility

Keep command handlers focused on a single aggregate root when possible:

```csharp
// Good: Single aggregate root
public record AddItemCommand([Key] Guid OrderId, Guid ProductId, int Quantity)
{
    public object Handle(Order order) =>
        order.AddItem(ProductId, Quantity);
}

// Consider refactoring: Multiple concerns would require manual loading
```### Event Source ID Clarity

Make it clear which property serves as the event source ID:

```csharp
// Clear and explicit
public record UpdateOrderCommand(EventSourceId OrderId, string Status);  // Obviously the event source ID

// Also clear with Key attribute
public record UpdateOrderCommand([Key] Guid OrderId, string Status);  // Marked as the key
```

### Validation

Validate that the event source ID is meaningful before processing:

```csharp
public record UpdateOrderCommand([Key] Guid OrderId, string Status)
{
    public object Handle(Order order)
    {
        if (order.IsDeleted)
        {
            throw new OrderAlreadyDeletedException(OrderId);
        }

        order.UpdateStatus(Status);
        return new OrderUpdated { Status = Status };
    }
}
```## Advanced Scenarios

### Custom Aggregate Root Resolution

If you need custom resolution logic, you can bypass the automatic injection and use `IAggregateRootFactory` directly:

```csharp
public record SomeCommand(string SomeProperty)
{
    public object Handle(IAggregateRootFactory aggregateRootFactory, CommandContext commandContext)
    {
        var customEventSourceId = DetermineCustomEventSourceId();
        var aggregate = aggregateRootFactory.Get<MyAggregate>(customEventSourceId).GetAwaiter().GetResult();

        // Process with custom-loaded aggregate
        return new SomeEvent();
    }

    private EventSourceId DetermineCustomEventSourceId() => EventSourceId.New();
}
```### Conditional Aggregate Loading

```csharp
public record ConditionalCommand([Key] Guid OrderId, bool ShouldProcessOrder)
{
    public object Handle(IServiceProvider serviceProvider)
    {
        if (ShouldProcessOrder)
        {
            // Only resolve Order when needed
            var order = serviceProvider.GetRequiredService<Order>();
            order.Process();
            return new OrderProcessed();
        }

        return new CommandIgnored();
    }
}
```
