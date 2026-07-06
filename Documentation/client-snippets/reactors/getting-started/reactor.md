```csharp
public interface ReactorEmailGateway
{
    Task SendOrderPlaced(string email, decimal amount, DateTimeOffset occurred);
}

public class OrderNotificationsReactor(ReactorEmailGateway emailGateway) : IReactor
{
    public Task Placed(ReactorOrderPlaced @event, EventContext context) =>
        emailGateway.SendOrderPlaced(
            @event.CustomerEmail,
            @event.TotalAmount,
            context.Occurred);
}
```
