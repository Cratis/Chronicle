<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/hosting.md -->
## In a host

Verified at **Cratis/Chronicle v19.50.0**, `ChronicleHostApplicationBuilderExtensions.cs`,
`ChronicleClientServiceCollectionExtensions.cs`, and
`Source/Clients/Connections/ServiceCollectionExtensions.cs`.

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddCratisChronicle(options => options.EventStore = "<EventStoreName>");
builder.Services.AddHostedService<<WorkerName>>();
await builder.Build().RunAsync();
```

`AddCratisChronicle` is an extension on **`IHostApplicationBuilder`**
(`Source/Clients/DotNET/ChronicleHostApplicationBuilderExtensions.cs:28`) and binds
the `Cratis:Chronicle` configuration section by default (`:25`, `:37`), with
`ValidateDataAnnotations().ValidateOnStart()`. The bound type is
`ChronicleClientOptions : ChronicleOptions`, which adds a `[Required] EventStore`
and an optional `EventStoreNamespaceResolverType`.

> **`AddCratisChronicle` on `IServiceCollection` does not exist**, despite what
> `Documentation/clients/dotnet/getting-started.md:61` shows. The real extensions
> are on `IHostApplicationBuilder`, `WebApplicationBuilder` (in the AspNetCore
> package), and Aspire's `IDistributedApplicationBuilder`. `IHostBuilder.AddCratisChronicle()`
> also exists but only registers concept type converters
> (`Source/Clients/DotNET/Hosting/HostBuilderExtensions.cs:18-23`) — it is not
> the wiring entry point. `IServiceCollection` has `AddCratisChronicleClient`
and `AddCratisChronicleConnection` instead.

`IChronicleBuilder` extensions are exactly five:
`WithArtifactsProvider`, `WithIdentityProvider`, `WithCorrelationIdAccessor`,
`WithNamespaceResolver`, `WithCamelCaseNamingPolicy`
(`Source/Clients/DotNET/ChronicleBuilderExtensions.cs`). **There is no
`WithClaimsBasedNamespaceResolver`**, despite a doc comment at
`ChronicleOptions.cs:141` referring to one. Pass the resolver instead:
`new ChronicleClient(options, namespaceResolver: new ClaimsBasedNamespaceResolver("tenant_id"))`.

A host starts even when the kernel is unavailable. Calls while disconnected fail
fast with `ConnectionTimedOut` or, after backoff, `ConnectionUnavailable`.
ASP.NET's `AddChronicleHealthCheck(HealthStatus)` exposes the connection health
under `chronicle` and honors the selected failure status; keep startup failure
logs visible. Host wiring registers `IEventSources` scoped.

### Development credentials are persisted

At **v19.50.0**, ClientCredentials with both username and password absent uses
`chronicle-dev-client` / `chronicle-dev-secret`:
`Source/Clients/DotNET/ChronicleClient.cs:334-345` and
`Source/Clients/Connections/ServiceCollectionExtensions.cs:57-68`.
`Source/Kernel/Core/Setup/Authentication/AuthenticationService.cs:170-208`
creates that application **directly in storage** only under `#if DEVELOPMENT`.
This is a Debug-build symbol, not an ASP.NET environment setting
(`Documentation/hosting/production.md`, "What makes a build a production build").
A production kernel reusing development storage can therefore still accept the
known development credentials. Remove the application from storage and, if
configured, from bootstrap configuration (configured applications are recreated
on startup). Changing the image or supplying new client credentials alone does
not remove that old application. See **cratis-chronicle-hosting** for kernel
certificates, private h2c and production setup.
