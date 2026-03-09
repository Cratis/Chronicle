# Structural Dependencies

`ChronicleOptions` is designed to hold **runtime configuration** that can be bound from `appsettings.json` or environment variables — connection strings, timeouts, TLS settings, naming policies, and so on.

Services and providers that must be resolved at registration time (before the DI container is built) are called **structural dependencies**. These are not configuration values and cannot be meaningfully bound from `appsettings.json`. They are passed directly as constructor arguments to `ChronicleClient` or set on `IChronicleBuilder` in the ASP.NET Core integration.

## Structural dependencies

| Dependency | Purpose | Default |
|---|---|---|
| `IClientArtifactsProvider` | Discovers event types, projections, reactors, reducers, and other artifacts at startup | `DefaultClientArtifactsProvider` (auto-discovers from loaded assemblies) |
| `IIdentityProvider` | Supplies the current user's identity for event metadata | `BaseIdentityProvider` (empty identity) |
| `ICorrelationIdAccessor` | Provides the current correlation ID for event metadata | `CorrelationIdAccessor` (generates a new ID per call) |
| `IEventStoreNamespaceResolver` | Resolves the event store namespace for each operation | `DefaultEventStoreNamespaceResolver` (always returns the default namespace) |
| `ILoggerFactory` | Creates loggers for the Chronicle client internals | `LoggerFactory` (no-op) |

## Passing to ChronicleClient directly

For console applications or other non-DI scenarios, pass structural dependencies as named constructor parameters:

```csharp
var client = new ChronicleClient(
    options,
    artifactsProvider: myProvider,
    loggerFactory: LoggerFactory.Create(b => b.AddConsole()));
```

All parameters are optional. Any parameter you omit uses the default shown in the table above.

## Configuring in ASP.NET Core with IChronicleBuilder

In ASP.NET Core applications, use the `configure` callback on `AddCratisChronicle` to set structural dependencies via the `IChronicleBuilder` fluent API. This callback runs at registration time, before the DI container is built.

```csharp
builder.AddCratisChronicle(
    configureOptions: options =>                         // runtime config — bindable from appsettings.json
    {
        options.EventStore = "my-store";
        options.ConnectionString = "chronicle://server:35000";
    },
    configure: b => b                                    // structural dependencies
        .WithArtifactsProvider(myCustomProvider)
        .WithIdentityProvider(myIdentityProvider)
        .WithLoggerFactory(myLoggerFactory));
```

The two callbacks are intentionally separate:
- `configureOptions` feeds the ASP.NET Core options pipeline and can be overridden by `appsettings.json` or `IConfiguration`.
- `configure` sets structural dependencies at registration time and is not overridable by configuration.

## IChronicleBuilder fluent methods

| Method | Sets |
|---|---|
| `WithArtifactsProvider(IClientArtifactsProvider)` | Custom artifact discovery |
| `WithIdentityProvider(IIdentityProvider)` | Custom identity resolution |
| `WithCorrelationIdAccessor(ICorrelationIdAccessor)` | Custom correlation ID accessor |
| `WithNamespaceResolver(IEventStoreNamespaceResolver)` | Custom namespace resolution |
| `WithLoggerFactory(ILoggerFactory)` | Custom logger factory |

## Custom artifact discovery

Implement `IClientArtifactsProvider` when you need to control exactly which types Chronicle discovers. This is useful in modular applications or when using assembly scanning that differs from the default:

```csharp
public class MyArtifactsProvider : IClientArtifactsProvider
{
    public IEnumerable<Type> EventTypes => [typeof(BookBorrowed), typeof(BookReturned)];
    public IEnumerable<Type> Projections => [typeof(BorrowedBooksProjection)];
    public IEnumerable<Type> Reactors => [];
    // ... other members
}
```

```csharp
builder.AddCratisChronicle(
    configureOptions: options => options.EventStore = "my-store",
    configure: b => b.WithArtifactsProvider(new MyArtifactsProvider()));
```

## DefaultClientArtifactsProvider

`DefaultClientArtifactsProvider` scans loaded assemblies for artifacts at first access (lazy initialization). You can construct it with a custom assembly provider:

```csharp
var assembliesProvider = new CompositeAssemblyProvider(
    ProjectReferencedAssemblies.Instance,
    PackageReferencedAssemblies.Instance);

var artifactsProvider = new DefaultClientArtifactsProvider(assembliesProvider);
```

The static `DefaultClientArtifactsProvider.Default` instance is used when no provider is supplied.

> **Note:** `DefaultClientArtifactsProvider` initializes lazily on first property access; no explicit initialization is required.
