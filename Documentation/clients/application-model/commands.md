# Commands

Commands in the Application Model client provide flexible patterns for handling business operations and returning responses. The client supports various return types including events, tuples with values, and automatic event source ID generation.

## Overview

Command handlers in the Application Model client can return different types of responses:

- **Single Events**: Return a single event to be appended to the event log
- **Multiple Events**: Return a collection of events to be appended
- **Tuples with Values**: Return both an event and a value, where the value becomes the command response
- **Event Source ID from Responses**: Use values from responses as event source IDs for subsequent operations

## Error Handling

### Exception-Free Command Handlers

Rather than throwing exceptions from a command handler for business rule violations or validation errors, you could  use `OneOf<>` types to return either validation results for failures or successful outcomes with events. This approach provides:

- **Type Safety**: Explicit handling of success and failure cases
- **Better Testability**: Clear return types make testing scenarios easier
- **Improved Maintainability**: No hidden exception paths to consider
- **Client-Friendly**: Structured error responses instead of exceptions

### When to Use Exceptions vs OneOf

**Use OneOf for:**

- Business rule violations (insufficient funds, invalid state transitions)
- Validation errors (missing required fields, invalid formats)
- Expected failure scenarios that clients should handle

**Exceptions should only be used for:**

- Unexpected technical failures (database connection issues, network timeouts)
- Programming errors (null reference, index out of range)
- Infrastructure problems that cannot be recovered from

## Response Value Handlers

The Application Model client includes two main response value handlers:

- `SingleEventCommandResponseValueHandler`: Handles single event responses
- `EventsCommandResponseValueHandler`: Handles collections of events

These handlers automatically append events to the event log using the event source ID and other metadata from the command context.

## Event Metadata

Commands can be decorated with metadata attributes that provide additional context when events are appended. This metadata helps organize and categorize events for better querying and processing.

### Available Metadata Attributes

Chronicle supports the following event metadata attributes:

- **EventSourceType**: Specifies the overarching entity or domain concept (e.g., "Account", "Customer")
- **EventStreamType**: Identifies the specific process or workflow (e.g., "Onboarding", "Transactions")
- **EventStreamId**: Provides a marker for separating independent streams (e.g., "Monthly", "Yearly")

### Using Metadata Attributes

Simply decorate your command with the desired metadata attributes:

```csharp
[EventSourceType("Account")]
[EventStreamType("Onboarding")]
[EventStreamId("Monthly")]
public record ProcessAccountOnboardingCommand([Key] Guid AccountId)
{
    public AccountOnboardingProcessed Handle()
    {
        return new AccountOnboardingProcessed
        {
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

All events generated from this command will automatically be tagged with the specified metadata.

### Dynamic Event Stream ID

For scenarios where the event stream ID needs to be determined at runtime, implement the `ICanProvideEventStreamId` interface:

```csharp
[EventSourceType("Order")]
[EventStreamType("Processing")]
public record ProcessTenantOrderCommand(Guid TenantId, [Key] Guid OrderId) 
    : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => TenantId.ToString();

    public TenantOrderProcessed Handle()
    {
        return new TenantOrderProcessed
        {
            TenantId = TenantId,
            OrderId = OrderId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
```

**Important**: You must use either the `[EventStreamId]` attribute or the `ICanProvideEventStreamId` interface, but not both. Using both will throw an `AmbiguousEventStreamId` exception.

### How Metadata Works

The Application Model client uses specialized value providers to extract metadata from commands:

1. `EventSourceTypeValuesProvider` - Extracts event source type from the `[EventSourceType]` attribute
2. `EventStreamTypeValuesProvider` - Extracts event stream type from the `[EventStreamType]` attribute
3. `EventStreamIdValuesProvider` - Extracts event stream ID from the `[EventStreamId]` attribute or `ICanProvideEventStreamId` interface

These values are stored in the command context and automatically included when events are appended to the event log.

### Learn More

For detailed information about each metadata type:

- [Event Source Type](event-source-type.md) - Learn about event source types
- [Event Stream Type](event-stream-type.md) - Understand event stream types
- [Event Stream ID](event-stream-id.md) - Explore event stream identifiers
- [Event Metadata Tags](../../concepts/event-metadata-tags.md) - Complete overview of metadata tags

## Single Event Responses

The simplest pattern is returning a single event from your command handler:

```csharp
[Command]
public record CreateUser(string Email, string Name)
{
    public UserCreated Handle(User user)
    {
        user.Create(Email, Name);

        // Return a single event
        return new UserCreated
        {
            Email = Email,
            Name = Name
        };
    }
}
```

The returned event will be automatically appended to the event log using the event source ID resolved from the command context.

## Multiple Events

You can return multiple events from a single command handler:

```csharp
[Command]
public record ProcessOrder([Key] Guid OrderId)
{
    public IEnumerable<object> Handle(Order order)
    {
        order.Process();

        // Return multiple events
        return new object[]
        {
            new OrderProcessed { ProcessedAt = DateTime.UtcNow },
            new OrderStatusChanged { NewStatus = "Processing" },
            new InventoryReserved { OrderId = OrderId }
        };
    }
}
```

All events in the collection will be appended to the event log in the order they appear in the collection.

## Tuple Responses with Values

One of the most powerful patterns is returning tuples that contain both events and values. The value part of the tuple becomes the command response, while the event is still appended to the event log:

```csharp
[Command]
public record CreateProduct(string Name, decimal Price)
{
    public (ProductCreated, Guid) Handle()
    {
        var productId = Guid.NewGuid();

        var productCreated = new ProductCreated
        {
            Name = Name,
            Price = Price
        };

        // Return both event and the generated ID
        return (productCreated, productId);
    }
}
```

In this example:

- The `ProductCreated` event is appended to the event log
- The `Guid` value is returned as the command response
- The client receives the generated product ID

## Using Response Values as Event Source IDs

A particularly useful pattern is when the response value can serve as the event source ID. This is especially valuable for creation scenarios:

```csharp
[Command]
public record CreateOrder(string CustomerName, List<OrderItem> Items)
{
    public (OrderCreated, EventSourceId) Handle()
    {
        // Generate a new event source ID for the order
        var orderId = EventSourceId.New();

        var orderCreated = new OrderCreated
        {
            CustomerName = CustomerName,
            Items = Items,
            CreatedAt = DateTime.UtcNow
        };

        // Return both event and the event source ID
        return (orderCreated, orderId);
    }
}
```

The `EventSourceId` in the tuple can be used by the client for subsequent operations on the same aggregate.

## Complex Tuple Responses

You can return more complex tuples with multiple values:

```csharp
[Command]
public record ProcessPayment([Key] Guid PaymentId, decimal Amount)
{
    public (PaymentProcessed, PaymentResult) Handle(Payment payment)
    {
        var result = payment.Process(Amount);

        var paymentProcessed = new PaymentProcessed
        {
            Amount = Amount,
            ProcessedAt = DateTime.UtcNow,
            Success = result.Success
        };

        var paymentResult = new PaymentResult
        {
            TransactionId = result.TransactionId,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };

        return (paymentProcessed, paymentResult);
    }
}
```

## Event Source ID Resolution in Responses

When using tuples, the Application Model client can also resolve event source IDs from the response values. This works with the same resolution logic as commands:

```csharp
[Command]
public record LinkAccounts(Guid PrimaryAccountId, Guid SecondaryAccountId)
{
    public (AccountsLinked, EventSourceId) Handle()
    {
        var accountsLinked = new AccountsLinked
        {
            PrimaryAccountId = PrimaryAccountId,
            SecondaryAccountId = SecondaryAccountId
        };

        // The primary account becomes the event source for this operation
        return (accountsLinked, PrimaryAccountId.ToString());
    }
}
```

## Response Value Handler Implementation

### SingleEventCommandResponseValueHandler

This handler:

- Checks if the response value is a single object that corresponds to a known event type
- Verifies that an event source ID is available in the command context
- Appends the single event to the event log

### EventsCommandResponseValueHandler

This handler:

- Checks if the response value is an `IEnumerable<object>` where all objects are known event types
- Verifies that an event source ID is available in the command context
- Appends all events to the event log in order

## Error Handling

### Unknown Event Types

Events must be registered with the event type system:

```csharp
// This event must be known to the IEventTypes service
public class UnknownEvent
{
    public string Data { get; set; }
}

public record SomeCommand(string Data)
{
    public UnknownEvent Handle()
    {
        // This will fail if UnknownEvent is not registered
        return new UnknownEvent { Data = Data };
    }
}
```

## Best Practices

### Clear Return Types

Be explicit about what your command handlers return:

```csharp
// Good: Clear single event return
public UserCreated Handle(CreateUserCommand command)

// Good: Clear multiple events return
public IEnumerable<object> Handle(ProcessOrderCommand command)

// Good: Clear tuple with value return
public (ProductCreated, Guid) Handle(CreateProductCommand command)
```

### Meaningful Response Values

When returning tuples, make the non-event values meaningful to the client:

```csharp
// Good: Client gets useful information
[Command]
public record CreateOrder(List<OrderItem> Items)
{
    public (OrderCreated, OrderConfirmation) Handle()
    {
        var orderCreated = new OrderCreated { Items = Items };
        var confirmation = new OrderConfirmation
        {
            OrderNumber = GenerateOrderNumber(),
            EstimatedDelivery = CalculateDeliveryDate(),
            TotalAmount = Items.Sum(i => i.Price * i.Quantity)
        };

        return (orderCreated, confirmation);
    }

    private static string GenerateOrderNumber() => Guid.NewGuid().ToString("N")[..8];
    private static DateTime CalculateDeliveryDate() => DateTime.UtcNow.AddDays(3);
}

// Less useful: Generic return values
[Command]
public record CreateSimpleOrder()
{
    public (OrderCreated, bool) Handle()
    {
        return (new OrderCreated(), true); // What does true mean?
    }
}
```

### Business Rule Validation

Command handlers should not throw exceptions for business rule violations. Instead, use `OneOf<>` to return either validation results or successful outcomes. This provides a more explicit and type-safe approach to error handling.

#### Using OneOf with Validation Results

```csharp
using Cratis.Applications.Validation;

[Command]
public record TransferFunds([Key] Guid AccountId, decimal Amount, Guid ToAccountId)
{
    public OneOf<ValidationResult, (FundsTransferred, TransferResult)> Handle(Account account, AccountSummary summary)
    {
        // Validate business rules first
        if (summary.Balance < Amount)
        {
            return ValidationResult.Error($"Insufficient funds. Cannot transfer {Amount:C}. Available balance: {summary.Balance:C}");
        }

        // Perform the business operation
        account.Transfer(ToAccountId, Amount);

        // Return successful result
        var transferredEvent = new FundsTransferred { Amount = Amount, ToAccount = ToAccountId };
        var result = new TransferResult { Success = true, NewBalance = summary.Balance - Amount };

        return (transferredEvent, result);
    }
}
```

#### Returning Single Events with Validation

For commands that return single events, use `OneOf<ValidationResult, Event>`:

```csharp
using Cratis.Applications.Validation;

[Command]
public record CreateUser(string Email, string Name)
{
    public OneOf<ValidationResult, UserCreated> Handle(IUserService userService)
    {
        // Validate business rules
        if (userService.EmailExists(Email))
        {
            return ValidationResult.Error("A user with email '{Email}' already exists");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("User name cannot be empty");
        }

        // Return successful event
        return new UserCreated
        {
            Email = Email,
            Name = Name
        };
    }
}
```

#### Returning Multiple Events with Validation

For commands that return multiple events, use `OneOf<ValidationResult, IEnumerable<object>>`:

```csharp
using Cratis.Applications.Validation;

[Command]
public record ProcessOrder([Key] Guid OrderId)
{
    public OneOf<ValidationResult, IEnumerable<object>> Handle(Order order, OrderSummary summary)
    {
        // Validate order can be processed
        if (summary.Status == OrderStatus.Cancelled)
        {
            return ValidationResult.Error("Order {OrderId} has been cancelled and cannot be processed");
        }

        if (summary.ItemCount == 0)
        {
            return ValidationResult.Error("Order must contain at least one item");
        }

        // Return successful events
        var events = new List<object>
        {
            new OrderProcessed { ProcessedAt = DateTime.UtcNow },
            new OrderStatusChanged { NewStatus = "Processing" }
        };

        if (summary.TotalAmount > 1000m)
        {
            events.Add(new HighValueOrderDetected { Amount = summary.TotalAmount });
        }

        return events;
    }
}
```

## Advanced Scenarios

### Conditional Event Generation

```csharp
[Command]
public record ProcessOrder([Key] Guid OrderId)
{
    public IEnumerable<object> Handle(Order order, OrderSummary summary)
    {
        var events = new List<object>();

        // Always add the main event
        events.Add(new OrderProcessed { ProcessedAt = DateTime.UtcNow });

        // Conditional events based on business logic
        if (summary.TotalAmount > 1000m)
        {
            events.Add(new HighValueOrderDetected { Amount = summary.TotalAmount });
        }

        if (summary.CustomerTier == CustomerTier.Premium)
        {
            events.Add(new PremiumOrderProcessed { CustomerId = summary.CustomerId });
        }

        return events;
    }
}
```

### Dynamic Response Types

```csharp
[Command]
public record Flexible(string Data, bool ReturnEvent, bool ReturnMultiple, bool ReturnTuple, string ResponseValue)
{
    public object Handle()
    {
        if (ReturnEvent)
        {
            return new SomeEvent { Data = Data };
        }

        if (ReturnMultiple)
        {
            return new object[]
            {
                new FirstEvent(),
                new SecondEvent()
            };
        }

        if (ReturnTuple)
        {
            return (new SomeEvent(), ResponseValue);
        }

        // Return non-event response (handled by other response handlers)
        return new { Success = true, Data = Data };
    }
}
```
