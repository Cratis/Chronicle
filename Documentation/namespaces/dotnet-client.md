# DotNET client usage

The .NET client resolves a namespace for every event store operation. You can configure namespace resolution by passing an `IEventStoreNamespaceResolver` implementation to the `ChronicleClient` constructor.

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
This is the default when no resolver is supplied.

### ClaimsBasedNamespaceResolver

Resolves the namespace from the current principal's claims. This is useful when you have an authenticated user and want to map the tenant from a claim.

```csharp
// Uses the default claim type "tenant_id"
var client = new ChronicleClient(
    options,
    namespaceResolver: new ClaimsBasedNamespaceResolver("tenant_id"));

// Uses a custom claim type
var client = new ChronicleClient(
    options,
    namespaceResolver: new ClaimsBasedNamespaceResolver("custom_tenant_claim"));
```

If the claim is not found or the user is not authenticated, the default namespace is used.

## Passing a resolver to ChronicleClient

For console or direct-client scenarios, pass the resolver as a constructor parameter:

```csharp
var resolver = new DefaultEventStoreNamespaceResolver();
var client = new ChronicleClient(options, namespaceResolver: resolver);
```

## Configuring in a hosted application

When using `IHostApplicationBuilder` (worker service or web app), configure the resolver through `ChronicleClientOptions` or directly via `IChronicleBuilder`:

```csharp
// Via options (type is resolved from DI)
builder.AddCratisChronicle(options =>
{
    options.EventStore = "my-store";
    options.EventStoreNamespaceResolverType = typeof(TenantNamespaceResolver);
});

// Via builder (instance is used directly)
builder.AddCratisChronicle(
    configureOptions: options => options.EventStore = "my-store",
    configure: b => b.WithNamespaceResolver(new TenantNamespaceResolver(config)));
```

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
var client = new ChronicleClient(options, namespaceResolver: new TenantNamespaceResolver(tenantContext));
```

## Best practices

- Keep resolvers deterministic within a request or operation
- Provide a safe fallback to the default namespace
- Avoid expensive resolution logic, as it is called frequently

For web applications, see [ASP.NET Core namespace resolution](aspnetcore.md).

