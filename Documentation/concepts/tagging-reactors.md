# Tagging Reactors

Tags provide a way to organize and categorize your reactors for better discoverability and management. By applying the `[Tag]` attribute to your reactor classes, you can assign one or more tags that describe the purpose or domain of the reactor.

## Adding Tags

You can tag reactors in multiple ways:

### Single Tag

Apply a single tag to your reactor:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

[Tag("Notifications")]
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

### Multiple Tags (Single Attribute)

Use the params feature to specify multiple tags in a single attribute:

```csharp
[Tag("Notifications", "Customer", "Email")]
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

### Multiple Tags (Multiple Attributes)

Apply multiple `[Tag]` attributes:

```csharp
[Tag("Integration")]
[Tag("ExternalAPI")]
[Tag("Inventory")]
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
[Tag("Notifications", "SMS")]
[Tag("Customer")]
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

- **Use meaningful names**: Choose tag names that clearly describe the purpose or domain
- **Be consistent**: Establish tag naming conventions across your organization
- **Don't over-tag**: Apply only relevant tags; too many can reduce their usefulness
- **Group related reactors**: Use tags to group reactors that serve similar purposes

## Common Tag Examples

Here are some common patterns for tagging reactors:

```csharp
// By integration type
[Tag("Notifications")]
[Tag("ExternalAPI")]
[Tag("MessageQueue")]
[Tag("FileSystem")]

// By domain
[Tag("Sales")]
[Tag("Inventory")]
[Tag("Customer")]
[Tag("Shipping")]

// By communication channel
[Tag("Email")]
[Tag("SMS")]
[Tag("Push")]
[Tag("Webhook")]

// By purpose
[Tag("Integration")]
[Tag("Alerting")]
[Tag("Monitoring")]
[Tag("Automation")]

// By stakeholder
[Tag("Customer")]
[Tag("Operations")]
[Tag("Finance")]
[Tag("Support")]
```

## Querying by Tag

Tags stored in the event store definition can be used for:

- Filtering and searching for specific reactors
- Organizing reactors in administrative interfaces
- Generating documentation
- Managing reactor deployments by tag
- Monitoring and alerting based on tag groups
- Controlling reactor activation by tag

Note: The specific querying capabilities depend on your Chronicle setup and tooling.

## See Also

- [Tagging](tagging.md) - Comprehensive guide to tagging in Chronicle
- [Reducers Tagging](../reducers/tagging-reducers.md) - Tagging reducers

