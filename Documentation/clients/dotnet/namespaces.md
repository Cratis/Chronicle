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

Use namespaces to isolate data for different customers or organizations:

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

### Environment-Based Isolation

Separate data based on the deployment environment:

```csharp
public class EnvironmentNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IHostEnvironment _environment;

    public EnvironmentNamespaceResolver(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public EventStoreNamespaceName Resolve()
    {
        return _environment.EnvironmentName.ToLowerInvariant();
    }
}
```

### Region-Based Partitioning

Partition data by geographic region:

```csharp
public class RegionNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IRegionProvider _regionProvider;

    public RegionNamespaceResolver(IRegionProvider regionProvider)
    {
        _regionProvider = regionProvider;
    }

    public EventStoreNamespaceName Resolve()
    {
        return _regionProvider.GetCurrentRegion();
    }
}
```

## Best Practices

- **Consistency**: Ensure the namespace resolver returns consistent values within the same request or operation context
- **Performance**: Keep resolution logic lightweight as it's called frequently
- **Defaults**: Always provide a fallback to a default namespace when resolution fails
- **Thread Safety**: Make resolvers thread-safe if they maintain state
- **Validation**: Validate namespace names to ensure they meet your naming conventions
