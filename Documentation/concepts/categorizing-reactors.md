# Categorizing Reactors

Categories provide a way to organize and tag your reactors for better discoverability and management. By applying the `[Category]` attribute to your reactor classes, you can assign one or more categories that describe the purpose or domain of the reactor.

## Adding Categories

You can categorize reactors in multiple ways:

### Single Category

Apply a single category to your reactor:

```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

[Category("Notifications")]
public class OrderConfirmationReactor
{
    readonly IEmailService _emailService;

    public OrderConfirmationReactor(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task OnOrderPlaced(OrderPlaced @event, EventContext context)
    {
        await _emailService.SendOrderConfirmation(@event.CustomerId, @event.OrderId);
    }
}
```

### Multiple Categories (Single Attribute)

Use the params feature to specify multiple categories in a single attribute:

```csharp
[Category("Notifications", "Customer", "Email")]
public class CustomerNotificationReactor
{
    readonly IEmailService _emailService;

    public CustomerNotificationReactor(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task OnCustomerRegistered(CustomerRegistered @event, EventContext context)
    {
        await _emailService.SendWelcomeEmail(@event.Email, @event.Name);
    }
}
```

### Multiple Categories (Multiple Attributes)

Apply multiple `[Category]` attributes:

```csharp
[Category("Integration")]
[Category("ExternalAPI")]
[Category("Inventory")]
public class InventorySyncReactor
{
    readonly IInventoryApi _inventoryApi;

    public InventorySyncReactor(IInventoryApi inventoryApi)
    {
        _inventoryApi = inventoryApi;
    }

    public async Task OnProductStockChanged(ProductStockChanged @event, EventContext context)
    {
        await _inventoryApi.UpdateStock(@event.ProductId, @event.NewQuantity);
    }
}
```

### Mixed Approach

Combine both approaches:

```csharp
[Category("Notifications", "SMS")]
[Category("Customer")]
public class SmsNotificationReactor
{
    readonly ISmsService _smsService;

    public SmsNotificationReactor(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public async Task OnOrderShipped(OrderShipped @event, EventContext context)
    {
        await _smsService.SendShippingNotification(@event.PhoneNumber, @event.TrackingNumber);
    }
}
```

## Best Practices

- **Use meaningful names**: Choose category names that clearly describe the purpose or domain
- **Be consistent**: Establish category naming conventions across your organization
- **Don't over-categorize**: Apply only relevant categories; too many can reduce their usefulness
- **Group related reactors**: Use categories to group reactors that serve similar purposes

## Common Category Examples

Here are some common patterns for categorizing reactors:

```csharp
// By integration type
[Category("Notifications")]
[Category("ExternalAPI")]
[Category("MessageQueue")]
[Category("FileSystem")]

// By domain
[Category("Sales")]
[Category("Inventory")]
[Category("Customer")]
[Category("Shipping")]

// By communication channel
[Category("Email")]
[Category("SMS")]
[Category("Push")]
[Category("Webhook")]

// By purpose
[Category("Integration")]
[Category("Alerting")]
[Category("Monitoring")]
[Category("Automation")]

// By stakeholder
[Category("Customer")]
[Category("Operations")]
[Category("Finance")]
[Category("Support")]
```

## Querying by Category

Categories stored in the event store definition can be used for:

- Filtering and searching for specific reactors
- Organizing reactors in administrative interfaces
- Generating documentation
- Managing reactor deployments by category
- Monitoring and alerting based on category groups
- Controlling reactor activation by category

Note: The specific querying capabilities depend on your Chronicle setup and tooling.
