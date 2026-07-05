```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Builder;

[EventType]
public record NamespacesAspNetCoreItemAddedToCart(string ProductId, int Quantity);

public static class NamespacesAspNetCoreWebAppExample
{
    public static void ConfigureApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCratisChronicle(options =>
        {
            options.EventStore = "production-store";
            options.WithHttpHeaderNamespaceResolver("x-tenant-id");
        });

        var app = builder.Build();
        app.MapPost("/api/cart/{cartId}/items", async (string cartId, IEventLog eventLog) =>
        {
            var itemAdded = new NamespacesAspNetCoreItemAddedToCart(ProductId: "product-123", Quantity: 1);
            await eventLog.Append(cartId, itemAdded);
            return Microsoft.AspNetCore.Http.Results.Ok();
        });
        app.Run();
    }
}
```
