# Namespace Resolution in ASP.NET Core

The ASP.NET Core client provides flexible namespace resolution specifically designed for multi-tenant web applications. By default, it uses HTTP headers to determine the tenant/namespace, but you can configure custom resolvers to match your application's needs.

## Default Behavior: HTTP Header Resolution

The default namespace resolver in ASP.NET Core applications reads the tenant identifier from an HTTP header. This is a common pattern in multi-tenant SaaS applications where the tenant is identified in each request.

### HttpHeaderEventStoreNamespaceResolver

The built-in `HttpHeaderEventStoreNamespaceResolver` reads the namespace from a configurable HTTP header:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.NamespaceHttpHeader = "x-cratis-tenant-id"; // Default value
});
```

When a request includes the configured header, that value is used as the namespace:

```http
GET /api/orders HTTP/1.1
Host: api.example.com
x-cratis-tenant-id: customer-123
```

In this example, all Chronicle operations within this request will use the `customer-123` namespace.

If the header is not present, the default namespace is used.

## Configuring Custom Resolvers

You can configure custom namespace resolvers in two ways: by type or by instance.

### Type-Based Configuration

Configure a custom resolver type that will be instantiated with dependency injection:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStore = "my-event-store";
    options.EventStoreNamespaceResolverType = typeof(ClaimsBasedNamespaceResolver);
});
```

The resolver type must implement `IEventStoreNamespaceResolver` and can have dependencies injected:

```csharp
public class ClaimsBasedNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsBasedNamespaceResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public EventStoreNamespaceName Resolve()
    {
        var tenantClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id");
        
        return tenantClaim?.Value ?? EventStoreNamespaceName.Default;
    }
}
```

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

## Common Scenarios

### JWT Claims-Based Resolution

Extract tenant information from JWT claims:

```csharp
public class JwtTenantResolver : IEventStoreNamespaceResolver
{
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly ILogger<JwtTenantResolver> _logger;

    public JwtTenantResolver(
        IHttpContextAccessor httpContextAccessor,
        ILogger<JwtTenantResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public EventStoreNamespaceName Resolve()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }

        _logger.LogWarning("No tenant found in claims, using default namespace");
        return EventStoreNamespaceName.Default;
    }
}
```

Configure it:

```csharp
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.EventStoreNamespaceResolverType = typeof(JwtTenantResolver);
});
```

### Subdomain-Based Resolution

Resolve tenant from subdomain:

```csharp
public class SubdomainNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IHttpContextAccessor _httpContextAccessor;

    public SubdomainNamespaceResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public EventStoreNamespaceName Resolve()
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            return EventStoreNamespaceName.Default;
        }

        // Extract subdomain (e.g., "customer123" from "customer123.example.com")
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }

        return EventStoreNamespaceName.Default;
    }
}
```

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

Here's a complete example showing how to set up Chronicle with a custom namespace resolver:

```csharp
using Cratis.Chronicle;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Configure Chronicle with custom namespace resolution
builder.Services.AddCratisChronicleClient();
builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
{
    options.Url = builder.Configuration["Chronicle:Url"] ?? "http://localhost:9007";
    options.EventStore = "production-store";
    options.EventStoreNamespaceResolverType = typeof(JwtTenantResolver);
});

var app = builder.Build();

app.MapGet("/api/orders", async (IEventLog eventLog) =>
{
    // The namespace is automatically resolved from the JWT claim
    var events = await eventLog.GetAsync();
    return Results.Ok(events);
});

app.Run();
```
