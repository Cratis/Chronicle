# DotNET client usage

The .NET client resolves a namespace for every event store operation. You can configure namespace resolution through `ChronicleOptions` by supplying an `IEventStoreNamespaceResolver` implementation.

## IEventStoreNamespaceResolver

```csharp
public interface IEventStoreNamespaceResolver
{
    EventStoreNamespaceName Resolve();
}
```

The resolver returns the namespace name to use for the current context.

## Built-in resolvers

### DefaultEventStoreNamespaceResolver

Always returns the default namespace. Use this when you do not need tenant isolation.

```csharp
var options = new ChronicleOptions
{
    EventStoreNamespaceResolver = new DefaultEventStoreNamespaceResolver()
};
```

### ClaimsBasedNamespaceResolver

Resolves the namespace from the current principal's claims. This is useful when you have an authenticated user and want to map the tenant from a claim.

```csharp
var options = new ChronicleOptions()
    .WithClaimsBasedNamespaceResolver(); // Uses default claim type "tenant_id"

var options = new ChronicleOptions()
    .WithClaimsBasedNamespaceResolver("custom_tenant_claim");
```

If the claim is not found or the user is not authenticated, the default namespace is used.

## Custom resolvers

Implement a resolver when your namespace comes from a custom context, such as a tenant provider or a request-scoped service.

If your application uses Arc Tenancy, you can resolve the namespace from its tenant context. See [Arc Tenancy](xref:Arc.Tenancy).

```csharp
public class TenantNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly ITenantContext _tenantContext;

    public TenantNamespaceResolver(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public EventStoreNamespaceName Resolve() =>
        _tenantContext.CurrentTenantId;
}
```

```csharp
var options = new ChronicleOptions
{
    EventStoreNamespaceResolver = new TenantNamespaceResolver(tenantContext)
};
```

## Best practices

- Keep resolvers deterministic within a request or operation
- Provide a safe fallback to the default namespace
- Avoid expensive resolution logic, as it is called frequently

For web applications, see [ASP.NET Core namespace resolution](aspnetcore.md).

