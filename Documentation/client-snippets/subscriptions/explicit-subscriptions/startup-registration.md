```csharp
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class SubscriptionsExplicitStartupRegistration
{
    public static async Task Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddCratisChronicle(options => options.EventStore = "Quickstart");

        // Safe to call on every application startup
        var app = builder.Build();

        var eventStore = app.Services.GetRequiredService<IEventStore>();

        await eventStore.Subscriptions.Subscribe(
            "orders-from-fulfillment",
            "fulfillment-service",
            builder => builder.WithEventType<SubscriptionsExplicitShipmentDispatched>());

        await eventStore.Subscriptions.Subscribe(
            "inventory-from-warehouse",
            "warehouse-service",
            builder => builder.WithEventType<SubscriptionsExplicitStockAdjusted>());

        await app.RunAsync();
    }
}
```
