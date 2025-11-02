# Namespaces

Namespaces in Chronicle provide a way to logically partition data within an event store. They are particularly useful for multi-tenant scenarios where you need to isolate data for different tenants or organizational units.

## What is a Namespace?

A namespace is a logical container within an event store that isolates events, projections, and other data. Each namespace operates independently, allowing you to:

- Separate data for different tenants or customers
- Isolate development, staging, and production environments
- Organize data by business domains or teams
- Implement multi-region data segregation

When you work with Chronicle, all operations are scoped to a specific namespace. If no namespace is explicitly specified, the default namespace is used.

## Namespace Resolvers

Namespace resolvers determine which namespace to use for the current operation. Chronicle provides a pluggable architecture through the `IEventStoreNamespaceResolver` interface, allowing you to implement custom resolution strategies.

### IEventStoreNamespaceResolver Interface

```csharp
public interface IEventStoreNamespaceResolver
{
    EventStoreNamespaceName Resolve();
}
```

This interface has a single method that returns the namespace name to use for the current context.

## Built-in Resolvers

Chronicle provides several built-in namespace resolvers:

### DefaultEventStoreNamespaceResolver

The simplest resolver that always returns the default namespace. This is useful when you don't need multi-tenancy or namespace isolation.

```csharp
var options = new ChronicleOptions
{
    Url = "http://localhost:9007",
    EventStoreNamespaceResolver = new DefaultEventStoreNamespaceResolver()
};
```

### ClaimsBasedNamespaceResolver

Resolves the namespace from the current user's claims using `ClaimsPrincipal.Current`. This is ideal for scenarios where you need tenant resolution based on authenticated user identity without requiring HTTP context.

```csharp
var options = new ChronicleOptions
{
    Url = "http://localhost:9007"
}.WithClaimsBasedNamespaceResolver(); // Uses default claim type "tenant_id"

// Or configure a custom claim type
var options = new ChronicleOptions
{
    Url = "http://localhost:9007",
    ClaimsBasedNamespaceResolverClaimType = "custom_tenant_claim"
}.WithClaimsBasedNamespaceResolver();
```

The resolver looks for the specified claim in the current principal's claims and returns its value as the namespace. If the claim is not found or the user is not authenticated, it returns the default namespace. The claim type is configured via the `ClaimsBasedNamespaceResolverClaimType` property, which defaults to "tenant_id".

### Custom Resolvers

You can create your own resolver by implementing the `IEventStoreNamespaceResolver` interface:

```csharp
public class TenantNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly ITenantContext _tenantContext;

    public TenantNamespaceResolver(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public EventStoreNamespaceName Resolve()
    {
        return _tenantContext.CurrentTenantId;
    }
}
```

Then configure it in your options:

```csharp
var options = new ChronicleOptions
{
    Url = "http://localhost:9007",
    EventStoreNamespaceResolver = new TenantNamespaceResolver(tenantContext)
};
```

## Common Use Cases

### Multi-Tenant SaaS Applications

For multi-tenant applications where tenant information is available in user claims, use the built-in `ClaimsBasedNamespaceResolver`:

```csharp
var options = new ChronicleOptions
{
    Url = "http://localhost:9007"
}.WithClaimsBasedNamespaceResolver();
```

This will automatically extract the tenant identifier from the user's claims and use it as the namespace. By default, it looks for a claim named "tenant_id", but you can configure a different claim type via the `ClaimsBasedNamespaceResolverClaimType` property.

### Custom Business Logic

For scenarios requiring custom business logic that isn't based on claims, implement your own resolver:

```csharp
public class CustomerNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly ICustomerContextProvider _contextProvider;

    public CustomerNamespaceResolver(ICustomerContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public EventStoreNamespaceName Resolve()
    {
        var customer = _contextProvider.GetCurrentCustomer();
        return customer?.Id ?? EventStoreNamespaceName.Default;
    }
}
```

## Best Practices

- **Consistency**: Ensure the namespace resolver returns consistent values within the same request or operation context
- **Performance**: Keep resolution logic lightweight as it's called frequently
- **Defaults**: Always provide a fallback to a default namespace when resolution fails
- **Thread Safety**: Make resolvers thread-safe if they maintain state
- **Validation**: Validate namespace names to ensure they meet your naming conventions
