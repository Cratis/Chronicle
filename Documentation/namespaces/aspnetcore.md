# ASP.NET Core namespace resolution

The ASP.NET Core client provides namespace resolution designed for multi-tenant web applications. It integrates with HTTP context and offers built-in resolvers for common request patterns.

If your multi-tenant setup is based on Arc Tenancy, you can map the current tenant to a namespace. See [Arc Tenancy](xref:Arc.Tenancy).

## Built-in resolvers

### HTTP header resolution (default)

The default resolver reads the namespace from an HTTP header. Configure it through `ChronicleAspNetCoreOptions`:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.NamespaceHttpHeader = "x-cratis-tenant-id"; // Default value
});

builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
    options.EventStore = "my-event-store"
           .WithHttpHeaderNamespaceResolver("x-cratis-tenant-id"));
```

When the header is present, its value becomes the namespace for the request. If the header is missing, the default namespace is used.

### Subdomain resolution

The subdomain resolver extracts the namespace from the request host (for example, `tenant1.example.com`).

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
    options.EventStore = "my-event-store"
           .WithSubdomainNamespaceResolver());
```

## Custom resolvers

You can configure a custom resolver either by type (DI) or by instance:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.EventStoreNamespaceResolverType = typeof(CustomNamespaceResolver);
});
```

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.EventStoreNamespaceResolver = new CustomNamespaceResolver(someConfiguration);
});
```

## Configuration priority

1. Instance configuration (`EventStoreNamespaceResolver`) when set to a non-default instance
2. Type configuration (`EventStoreNamespaceResolverType`) when provided
3. Default HTTP header resolver

## Example: Web app setup

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "production-store";
    options.WithHttpHeaderNamespaceResolver("x-tenant-id");
});

var app = builder.Build();
app.MapPost("/api/cart/{cartId}/items", async (string cartId, IEventLog eventLog) =>
{
    var itemAdded = new ItemAddedToCart(ProductId: "product-123", Quantity: 1);
    await eventLog.Append(cartId, itemAdded);
    return Results.Ok();
});
app.Run();

record ItemAddedToCart(string ProductId, int Quantity);
```

For non-web contexts, see [DotNET client usage](dotnet-client.md).

