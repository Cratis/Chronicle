```csharp
// OrderService/Program.cs
using Microsoft.AspNetCore.Builder;

public static class SubscriptionsImplicitConsumerSetup
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCratisChronicle(options => options.EventStore = "order-service");

        // Just reference the events from the NuGet package
        using var app = builder.Build();
        // Reactors/projections that observe FulfillmentService.Events types
        // are automatically routed to the inbox
    }
}
```
