# Namespace Resolution in ASP.NET Core

The ASP.NET Core client provides flexible namespace resolution specifically designed for multi-tenant web applications. Chronicle includes built-in resolvers for common scenarios, or you can implement custom resolvers to match your application's needs.

## Built-in Namespace Resolvers

### HTTP Header Resolution (Default)

The default namespace resolver in ASP.NET Core applications reads the tenant identifier from an HTTP header. This is a common pattern in multi-tenant SaaS applications where the tenant is identified in each request.

The built-in `HttpHeaderEventStoreNamespaceResolver` reads the namespace from a configurable HTTP header:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.NamespaceHttpHeader = "x-cratis-tenant-id"; // Default value
});

// Or use the extension method
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
    options.EventStore = "my-event-store"
           .WithHttpHeaderNamespaceResolver("x-cratis-tenant-id"));
```

When a request includes the configured header, that value is used as the namespace:

```http
GET /api/orders HTTP/1.1
Host: api.example.com
x-cratis-tenant-id: customer-123
```

In this example, all Chronicle operations within this request will use the `customer-123` namespace.

If the header is not present, the default namespace is used.

### Subdomain-Based Resolution

The built-in `SubdomainNamespaceResolver` extracts the namespace from the subdomain of the HTTP request host. This is ideal for applications using subdomains for tenant identification (e.g., `tenant1.example.com`).

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
    options.EventStore = "my-event-store"
           .WithSubdomainNamespaceResolver());
```

With this configuration:

- A request to `customer123.example.com` uses namespace `customer123`
- A request to `example.com` uses the default namespace
- A request to `www.example.com` uses the default namespace

## Configuring Custom Resolvers

For scenarios not covered by the built-in resolvers, you can configure custom namespace resolvers in two ways: by type or by instance.

### Type-Based Configuration

Configure a custom resolver type that will be instantiated with dependency injection:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.EventStoreNamespaceResolverType = typeof(CustomNamespaceResolver);
});
```

The resolver type must implement `IEventStoreNamespaceResolver` and can have dependencies injected through the constructor.

### Instance-Based Configuration

For more control, you can provide a pre-configured instance:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.EventStoreNamespaceResolver = new CustomNamespaceResolver(someConfiguration);
});
```

**Note**: Instance configuration takes precedence over type configuration, unless the instance is a `DefaultEventStoreNamespaceResolver`, in which case the type configuration is used.

## Custom Scenarios

### Route-Based Resolution

Extract tenant from URL path:

```csharp
public class RouteBasedNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IHttpContextAccessor _httpContextAccessor;

    public RouteBasedNamespaceResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public EventStoreNamespaceName Resolve()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.RouteValues.TryGetValue("tenantId", out var tenantId) == true)
        {
            return tenantId?.ToString() ?? EventStoreNamespaceName.Default;
        }

        return EventStoreNamespaceName.Default;
    }
}
```

This resolver works with routes like:

```csharp
app.MapGet("/api/{tenantId}/orders", (string tenantId) => { ... });
```

### Combined Strategy

Implement a fallback strategy that tries multiple resolution methods:

```csharp
public class MultiStrategyNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IHttpContextAccessor _httpContextAccessor;

    public MultiStrategyNamespaceResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public EventStoreNamespaceName Resolve()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return EventStoreNamespaceName.Default;
        }

        // Try header first
        if (context.Request.Headers.TryGetValue("x-tenant-id", out var headerValue))
        {
            return headerValue.ToString();
        }

        // Try claims second
        var claim = context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(claim))
        {
            return claim;
        }

        // Try route value
        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeValue))
        {
            return routeValue?.ToString() ?? EventStoreNamespaceName.Default;
        }

        return EventStoreNamespaceName.Default;
    }
}
```

## Configuration Priority

The ASP.NET Core client follows this priority order when determining which resolver to use:

1. **Instance Configuration**: If `EventStoreNamespaceResolver` is set to a non-default instance, it's used
2. **Type Configuration**: If no instance is configured (or it's set to `DefaultEventStoreNamespaceResolver`), the type specified in `EventStoreNamespaceResolverType` is instantiated
3. **Default**: If neither is configured, `HttpHeaderEventStoreNamespaceResolver` is used

## Best Practices

- **Validation**: Always validate tenant identifiers to ensure they're valid and the user has access
- **Security**: Verify that users can only access their own tenant's data
- **Logging**: Log namespace resolution for debugging and auditing purposes
- **Performance**: Keep resolution logic fast to avoid impacting request latency
- **Defaults**: Always provide a sensible default namespace for when resolution fails
- **Testing**: Write unit tests for your custom resolvers to ensure they handle edge cases

## Thread Safety

Namespace resolvers in ASP.NET Core should be registered as scoped services (default behavior). The `IHttpContextAccessor` is thread-safe and provides the correct context for the current request.

## Example: Complete Setup

Here's a complete example showing how to set up Chronicle with different built-in resolvers:

### Using HTTP Header Resolution (Default)

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "production-store";
    // HTTP header resolution is the default, but you can configure it explicitly
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
```

### Using Subdomain Resolution

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "production-store";
    options.WithSubdomainNamespaceResolver();
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
