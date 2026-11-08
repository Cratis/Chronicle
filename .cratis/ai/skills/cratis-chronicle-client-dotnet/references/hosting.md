<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/hosting.md -->
## In a host

Source: `Cratis/Chronicle`: `ChronicleHostApplicationBuilderExtensions.cs`,
`ChronicleClientServiceCollectionExtensions.cs`, and
`Source/Clients/Connections/ServiceCollectionExtensions.cs`.

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddCratisChronicle(options => options.EventStore = "<EventStoreName>");
builder.Services.AddHostedService<<WorkerName>>();
await builder.Build().RunAsync();
```

`AddCratisChronicle` is an extension on **`IHostApplicationBuilder`**
(`Source/Clients/DotNET/ChronicleHostApplicationBuilderExtensions.cs`) and binds
the `Cratis:Chronicle` configuration section by default, with
`ValidateDataAnnotations().ValidateOnStart()`. The bound type is
`ChronicleClientOptions : ChronicleOptions`, which adds a `[Required] EventStore`
and an optional `EventStoreNamespaceResolverType`.

> **`AddCratisChronicle` on `IServiceCollection` does not exist**, despite what
> `Documentation/clients/dotnet/getting-started.md` shows. The real extensions
> are on `IHostApplicationBuilder`, `WebApplicationBuilder` (in the AspNetCore
> package), and Aspire's `IDistributedApplicationBuilder`. `IHostBuilder.AddCratisChronicle()`
> also exists but only registers concept type converters
> (`Source/Clients/DotNET/Hosting/HostBuilderExtensions.cs`) — it is not
> the wiring entry point. `IServiceCollection` has `AddCratisChronicleClient`
and `AddCratisChronicleConnection` instead.

`IChronicleBuilder` extensions are exactly five:
`WithArtifactsProvider`, `WithIdentityProvider`, `WithCorrelationIdAccessor`,
`WithNamespaceResolver`, `WithCamelCaseNamingPolicy`
(`Source/Clients/DotNET/ChronicleBuilderExtensions.cs`). **There is no
`WithClaimsBasedNamespaceResolver`**, despite a doc comment at
`ChronicleOptions.cs` referring to one. Pass the resolver instead:
`new ChronicleClient(options, namespaceResolver: new ClaimsBasedNamespaceResolver("tenant_id"))`.

A host starts even when the kernel is unavailable. Calls while disconnected fail
fast with `ConnectionTimedOut` or, after backoff, `ConnectionUnavailable`.
ASP.NET's `AddChronicleHealthCheck(HealthStatus)` exposes the connection health
under `chronicle` and honors the selected failure status; keep startup failure
logs visible. Host wiring registers `IEventSources` scoped.

### Development credentials are persisted

ClientCredentials with both username and password absent uses
`chronicle-dev-client` / `chronicle-dev-secret`:
`Source/Clients/DotNET/ChronicleClient.cs` and
`Source/Clients/Connections/ServiceCollectionExtensions.cs`.
`Source/Kernel/Core/Setup/Authentication/AuthenticationService.cs`
creates that application **directly in storage** only under `#if DEVELOPMENT`.
This is a Debug-build symbol, not an ASP.NET environment setting
(`Documentation/hosting/production.md`, "What makes a build a production build").
A production kernel reusing development storage can therefore still accept the
known development credentials. Remove the application from storage and, if
configured, from bootstrap configuration (configured applications are recreated
on startup). Changing the image or supplying new client credentials alone does
not remove that old application. See **cratis-chronicle-hosting** for kernel
certificates, private h2c and production setup.

## Request completion ownership (19.110.1+ .NET packages)

Verified at **v19.110.1**: `Source/Clients/DotNET/Transactions/`
(`IUnitOfWorkCompletionOwnershipFeature.cs`, `IUnitOfWorkCompletionClaim.cs`,
`UnitOfWorkCompletionResult.cs`), `Source/Clients/AspNetCore/Transactions/UnitOfWorkMiddleware.cs`
and `Source/Clients/AspNetCore/ChronicleUnitOfWorkOptions.cs`.

Nested hosts can claim a request's completion with
`IUnitOfWorkCompletionOwnershipFeature.TryClaim`. Complete through the claim
and inspect its result before writing the response. An uncompleted claim is
rolled back by the outer middleware, never silently committed. This does not
grant arbitrary nested code the decision-read owner capability described in
**cratis-chronicle-decision-reads**. `UnitOfWorkCompletionOutcome.Unknown` means a
technical commit failure, not proof of rollback. Decision conflicts also occur
in `ConcurrencyViolations`; avoid reporting the same source's conflict twice.

Compatibility middleware logs a failed post-handler commit without changing a
response already sent. Opt in to problem responses **before headers start** with
`ChronicleAspNetCoreOptions.UnitOfWork.ReportFailuresBeforeResponseStarts`:
concurrency/decision conflict is 409, constraint violation is 400, other append
failure is 500. A started response cannot be repaired; complete through the
claim first when a success response must mean the write committed.
