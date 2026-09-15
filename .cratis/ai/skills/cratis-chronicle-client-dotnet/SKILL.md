---
name: cratis-chronicle-client-dotnet
description: Talk to a Chronicle server from a standalone .NET application with the Cratis.Chronicle client - connection strings, ChronicleClient construction outside any host, AddCratisChronicle for a worker or ASP.NET host, [EventType] records, IEventSequence.Append, reactors and reducers found by assembly scanning, the connection lifecycle and registration wait, and the client/server compatibility check. Use when a console, worker, or service app connects to Chronicle directly. Do not use for Arc applications, the Chronicle kernel, or the Kotlin, TypeScript, and Elixir clients.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/SKILL.md -->

# The Chronicle client for .NET

`Cratis.Chronicle` is a **standalone client SDK**. A console app, a worker, or a
service constructs a client, asks it for an event store, and appends or observes.
There is no host requirement and no application architecture imposed.

> **This is not the Arc integration.** In an Arc application, Chronicle is wired
> in for you and commands return events that Arc appends. Everything below is the
> layer underneath: what an application that is *not* Arc has to do itself.

## Verified product sources

This skill is verified against the `Cratis/Chronicle` repository at tag
`v17.0.1`, whose client sources are byte-identical to `16.45.2` for every public
type cited here except the compatibility check noted below.

| Package | Line | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `17.x` | `ChronicleClient`, `IEventStore`, `[EventType]`, observers |
| `Cratis.Chronicle.Connections` | `17.x` | `ChronicleConnectionString`, connection lifecycle |
| `Cratis.Chronicle.AspNetCore` | `17.x` | ASP.NET Core wiring, header/subdomain namespace resolvers |
| `Cratis.Chronicle.Testing` | `17.x` | in-process scenarios |
| `Cratis.Chronicle.CodeAnalysis` | `17.x` | the analyzers, a **separate opt-in** package |

Take the exact version from nuget.org. The version in source is a `1.0.0`
placeholder injected at pack time (`Source/Directory.Build.props`), so the
repository never carries the real number. Reverify before claiming support for a
version you have not checked.

## Which package

```shell
dotnet add package Cratis.Chronicle              # console / worker service
dotnet add package Cratis.Chronicle.AspNetCore   # ASP.NET Core
```

`Cratis.Chronicle.CodeAnalysis` is **not** pulled in by `Cratis.Chronicle`. Add
it deliberately — it is what warns about the mistakes an event model makes
silently, such as a nullable event property.

## Connecting

### The smallest real thing

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

using var client = new ChronicleClient(ChronicleConnectionString.Development);
var eventStore = await client.GetEventStore("<EventStoreName>");
```

That is the compiled documentation snippet verbatim
(`Documentation/client-snippets/get-started/console/connect.md`). Those snippet
files are compiled in CI against the real client projects, which makes them the
safest source of .NET Chronicle example code in existence — prefer them over
prose documentation.

`IChronicleClient` — `Source/Clients/DotNET/IChronicleClient.cs:11`:

| Member | Line |
| --- | --- |
| `ChronicleOptions Options { get; }` | `:16` |
| `ICausationManager CausationManager { get; }` | `:21` |
| `Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default)` | `:32` |
| `Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default)` | `:39` |
| `void EvictEventStores()` | `:53` |

**There is no `IChronicleClient.Connect`.** Connecting happens inside
`GetEventStore`, which discovers artifacts and then connects
(`Source/Clients/DotNET/ChronicleClient.cs:403-409`). When you do need the
connection explicitly — in a worker's `ExecuteAsync`, for instance — it is
`eventStore.Connection.Connect()`.

`ChronicleClient` is `IDisposable`
(`Source/Clients/DotNET/ChronicleClient.cs:30`) with five public constructors
(`:76`, `:85`, `:94`, `:110`, `:183`). The one that matters for a real
application is `:110`:

```csharp
public ChronicleClient(
    ChronicleOptions options,
    IClientArtifactsProvider? artifactsProvider = null,
    IServiceProvider? serviceProvider = null,
    IIdentityProvider? identityProvider = null,
    ICorrelationIdAccessor? correlationIdAccessor = null,
    IEventStoreNamespaceResolver? namespaceResolver = null,
    ILoggerFactory? loggerFactory = null,
    INamingPolicy? namingPolicy = null)
```

The canonical standalone sample uses it — `Samples/SimpleConsole/Program.cs:28-33`:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000");
options.DefaultSinkTypeId = sinkType;

using var client = new ChronicleClient(options, loggerFactory: loggerFactory);
var store = await client.GetEventStore("TestStoreCS");
```

### Options

`ChronicleOptions` — `Source/Clients/DotNET/ChronicleOptions.cs`. Statics:
`FromConnectionString(string)` (`:191`), `FromConnectionString(ChronicleConnectionString)`
(`:198`), `FromDevelopmentConnectionString()` (`:214`). The parameterless
constructor (`:40`) is the development connection string.

Options worth knowing: `AutoDiscoverAndRegister` (default `true`, `:90`),
`ConnectTimeout` (default 5 seconds, `:95`), `SkipKeepAlive` (`:127` — turn the
watchdog off for a short-lived client), `DefaultSinkTypeId` (default
`WellKnownSinkTypes.MongoDB`), `Tls`, `Authentication`, `RegistrationRetry`.

### Connection strings, not URLs

**There is no `ChronicleUrl` type and no microservice id.** The identity of a
connection is a connection string plus an event store name and optionally a
namespace. `ChronicleConnectionString` lives in `Cratis.Chronicle.Connections`
(`Source/Clients/Connections/ChronicleConnectionString.cs:33`):

```
chronicle://host[:port]/?opts
chronicle://user:pass@host[:port]/?opts
chronicle+srv://host/?opts
```

Default port `35000`. `Default` is `chronicle://localhost:35000` (`:48`);
`Development` adds the `chronicle-dev-client` / `chronicle-dev-secret`
credentials (`:57`). Query options: `apiKey`, `auth`, `skipTlsValidation`,
`loadBalancer`, `srvNameServer`, `certificatePath`, `certificatePassword`
(`ChronicleConnectionStringBuilder.cs:27-36`). There is an implicit conversion
from `string` (`:158`) and a `Redacted` form for logging (`:152`).

> **Certificate validation is skipped by default, and that is deliberate.** The
> Chronicle server always serves TLS, generating a self-signed certificate on
> every start when none is configured, so a development pair connects with no
> setup — the rationale is written out at `ChronicleClient.cs:141-146`. **A
> production connection string must set `skipTlsValidation=false`** (or
> `Tls.SkipCertificateValidation = false`) against a server whose certificate is
> actually verifiable.

### In a host

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
> the wiring entry point.

`IChronicleBuilder` extensions are exactly five:
`WithArtifactsProvider`, `WithIdentityProvider`, `WithCorrelationIdAccessor`,
`WithNamespaceResolver`, `WithCamelCaseNamingPolicy`
(`Source/Clients/DotNET/ChronicleBuilderExtensions.cs`). **There is no
`WithClaimsBasedNamespaceResolver`**, despite a doc comment at
`ChronicleOptions.cs:141` referring to one. Pass the resolver instead:
`new ChronicleClient(options, namespaceResolver: new ClaimsBasedNamespaceResolver("tenant_id"))`.

## The client and the server check each other

Since Chronicle 17 the compatibility check is a server-side RPC, and **the client
runs it automatically inside `Connect()`** —
`Source/Clients/Connections/ChronicleConnection.cs:310` calls
`CheckCompatibility` (`:405`), sending the client type, client version, protocol
version, and the descriptor set its contracts package was built with (`:411-417`).
The rationale is at `:400-404`: Chronicle has clients in four languages and only
some can build a descriptor set at runtime, so each ships the one it was built
with and the server does the single comparison.

Behavior you can rely on:

- A server too old to have the RPC answers `Unimplemented`, and the client falls
  back to the previous client-side exchange (`:419-425`) — upgrading the client
  does not silently drop the check.
- Any other transport error is **logged and ignored** (`:426-432`), on the stated
  reasoning that failing to ask says nothing about whether the two sides agree.
- A genuine mismatch throws `IncompatibleServerException`
  (`Source/Clients/Connections/IncompatibleServerException.cs:10`) whose message
  names the server address, its version, its protocol version, and the specific
  incompatibilities (`:436-443`).

The client identifies itself as `".NET"`
(`Source/Clients/Connections/ChronicleClientIdentity.cs:22`) with its assembly
informational version (`:27`) and the contracts protocol version (`:32`).

## Event types

```csharp
using Cratis.Chronicle.Events;

/// <summary><What happened, in the past tense.></summary>
[EventType]
public record <EventName>(<Type> <Property>);
```

`EventTypeAttribute` is in `Cratis.Chronicle.Events` —
`Source/Clients/DotNET/Events/EventTypeAttribute.cs:20`:

```csharp
public sealed class EventTypeAttribute(string id = "", uint generation = EventTypeGeneration.FirstValue) : Attribute
```

**Two arguments only** — `id` (empty means the type name) and `generation`
(`EventTypeGeneration.FirstValue` is `1U`). There is no `isPublic` parameter.
`AttributeUsage` is `AttributeTargets.Class`, which records satisfy.

For a new event, pass nothing: the type name is the identifier. Use `generation`
only when evolving a contract that already exists, and prefer
`EventTypeGenerationForAttribute<TEventType>` over a second `[EventType]` — the
guidance is in the attribute's own remarks (`EventTypeAttribute.cs:14-18`).

## Appending

`IEventLog : IEventSequence` is a marker
(`Source/Clients/DotNET/EventSequences/IEventLog.cs:9`); the surface is on
`IEventSequence` (`Source/Clients/DotNET/EventSequences/IEventSequence.cs:14`):

```csharp
Task<AppendResult> Append(                                   // :115
    EventSourceId eventSourceId,
    object @event,
    EventStreamType? eventStreamType = default,
    EventStreamId? eventStreamId = default,
    EventSourceType? eventSourceType = default,
    CorrelationId? correlationId = default,
    IEnumerable<string>? tags = default,
    ConcurrencyScope? concurrencyScope = default,
    DateTimeOffset? occurred = default,
    Subject? subject = default);

Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events, /* same optionals */);  // :144
Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, /* ... */);                            // :167
```

```csharp
var result = await eventStore.EventLog.Append(<eventSourceId>, new <EventName>(<value>));
if (!result.IsSuccess)
{
    // result.ConstraintViolations, result.ConcurrencyViolation, result.Errors
}
```

- `EventSourceId` is a `record EventSourceId(string Value) : ConceptAs<string>`
  with implicit conversion from `string` and `Guid`
  (`Source/Clients/DotNET/Events/EventSourceId.cs:12`, `:37`, `:44`).
- `AppendResult` carries `SequenceNumber`, `IsSuccess`,
  `HasConstraintViolations`, `HasConcurrencyViolations`, `HasErrors`,
  `ConstraintViolations`, `ConcurrencyViolation`, `Errors`, and the routing facts
  (`Source/Clients/DotNET/EventSequences/AppendResult.cs:13-59`).
- The `IEnumerable<EventForEventSourceId>` overload is the way to append across
  several event sources in one batch.
- **There is no `AppendAnonymous`.** It does not exist anywhere in the product.

Reading back is on the same interface: `GetForEventSourceIdAndEventTypes` (`:51`),
`HasEventsFor` (`:58`), `GetFromSequenceNumber` (`:67`), `GetNextSequenceNumber`
(`:73`), `GetTailSequenceNumber` (`:84`). Erasure is `Redact` (`:191`, `:200`).

Several appends as one unit:

```csharp
var unitOfWork = eventStore.UnitOfWorkManager.Begin(CorrelationId.New());
await eventStore.EventLog.Transactional.Append(<eventSourceId>, new <EventName>(<value>));
await unitOfWork.Commit();
```

`ITransactionalEventSequence` returns `Task`, not a result
(`Source/Clients/DotNET/EventSequences/ITransactionalEventSequence.cs:33`); the
results live on the unit of work.

## Observing

| Artifact | Shape | Source |
| --- | --- | --- |
| Reactor | marker `IReactor` (empty interface), `[Reactor]` optional | `Reactors/IReactor.cs:9`, `Reactors/ReactorAttribute.cs:14` |
| Reducer | `IReducerFor<TReadModel>` | `Reducers/IReducerFor.cs:10` |
| Projection | `IProjectionFor<TReadModel>` with `Define(IProjectionBuilderFor<TReadModel>)` | `Projections/IProjectionFor.cs:10-17` |
| Model-bound projection | attributes on the read model | `Projections/ModelBound/` |
| Read model reactor | marker `IReadModelReactor`, methods `Added`/`Modified`/`Removed` | `ReadModels/IReadModelReactor.cs:10-18` |

**A reactor handler is found by its first parameter's type**, and further
parameters are resolved as dependencies — `EventContext`, a read model, a service
(`Source/Clients/DotNET/Reactors/EventHandlerMethods.cs:21-46`, `:89-103`). The
return may be `Task`, `void`, or a side-effect event type.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class <ReactorName>(<IDependency> <dependency>) : IReactor
{
    public async Task <AnyMethodName>(<EventName> @event, EventContext context)
    {
        await <dependency>.<DoSomething>(context.EventSourceId, @event.<Property>);
    }
}
```

> **There is no `[Observer]` attribute and no `IObserver` client artifact.**
> "Observer" is the kernel's umbrella term for a projection, reducer, or reactor;
> it is not something you write. `Source/Clients/DotNET/Observation/` holds
> `ObserverId`, `ObserverRunningState`, and failed-partition types only.

Read models are queried through `eventStore.ReadModels`
(`Source/Clients/DotNET/ReadModels/IReadModels.cs`):
`GetInstanceById<TReadModel>(ReadModelKey, ReadModelSessionId?)` (`:38`),
`GetInstances<TReadModel>(EventCount?)` (`:55`),
`GetSnapshotsById<TReadModel>` (`:63`),
`IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>()` (`:70`).

## Discovery

Artifacts are found by **assembly scanning**, with no registration call and no DI
container required. `DefaultClientArtifactsProvider.Default` composes the
project-referenced and package-referenced assemblies
(`Source/Clients/DotNET/DefaultClientArtifactsProvider.cs:35`), and the
predicates are exactly (`:230-238`):

| Kind | Predicate |
| --- | --- |
| event types | `HasAttribute<EventTypeAttribute>()` or `HasAttribute<EventTypeGenerationForAttribute>()` |
| projections | `HasInterface(typeof(IProjectionFor<>))` |
| model-bound projections | `HasModelBoundProjectionAttributes()` |
| reactors | `HasInterface<IReactor>()` and not generic |
| read model reactors | `HasInterface<IReadModelReactor>()` and not generic |
| reducers | `HasInterface(typeof(IReducerFor<>))` and not generic |

Explicit registration is available per family as an alternative —
`IEventTypes.Register`, `IConstraints.Register`, `IProjections.Register`,
`IReducers.Register`, `IReactors.Register<TReactor>()`,
`IReadModels.Register<TReadModel>()` — and is what you use with
`AutoDiscoverAndRegister = false`.

## Lifecycle

- **Registration is wired to the connection, not called by you.**
  `EventStore.cs:265-268` subscribes `RegisterAll` to `Connection.Lifecycle.OnConnected`
  when `autoDiscoverAndRegister` is on, so a reconnect re-registers everything.
- `DiscoverAll()` does event types first, then constraints, reactors, reducers,
  projections, and seeding in parallel (`EventStore.cs:349-362`). `RegisterAll()`
  is single-flighted with jittered backoff and a background retry loop (`:373-379`).
- **Wait with `WaitForRegistration`, not by polling `IsConnected`.**
  `RegistrationWaitExtensions.WaitForRegistration(this IEventStore, TimeSpan? timeout = default)`
  (`Source/Clients/DotNET/Registrations/RegistrationWaitExtensions.cs:42`, default
  5 seconds) exists precisely for this, and its own remarks warn against the
  `IConnectionLifecycle.IsConnected` alternative (`:34-39`). Connected is not
  registered.
- **Keepalive is a bidirectional stream plus a watchdog.** The watchdog monitors
  every `MonitorIntervalMilliseconds = 1000`
  (`Source/Clients/Connections/ConnectionWatchdog.cs:34`) and a keepalive that
  falls more than 5 seconds behind is treated as a lost connection, with
  reconnect backoff capped at 30 seconds. **The failure mode is silence, not an
  exception** — appends keep working while observers go quiet.
- `Dispose()` disposes read model reactors for created stores, cancels the owned
  connection, and disposes the connection
  (`Source/Clients/DotNET/ChronicleClient.cs:225-246`).

## What a standalone app owns that a host would have supplied

Every one of these has a silent default. Read them as a checklist, because the
defaults are reasonable for a sample and wrong for a service
(`ChronicleClient.cs:121-127`):

| Concern | Default when you pass nothing |
| --- | --- |
| `IClientArtifactsProvider` | `DefaultClientArtifactsProvider.Default` — full assembly scan |
| `IServiceProvider` | `DefaultServiceProvider`, which activates everything through `Activator.CreateInstance` (`DefaultServiceProvider.cs:41`) |
| `IIdentityProvider` | `BaseIdentityProvider` |
| `ICorrelationIdAccessor` | `CorrelationIdAccessor` |
| `IEventStoreNamespaceResolver` | `DefaultEventStoreNamespaceResolver` — always `"Default"` |
| `ILoggerFactory` | `new LoggerFactory()` — **a silent one** |
| Configuration binding | none; there is no `Cratis:Chronicle` section without a host |

The `IServiceProvider` default is the sharp one: **a reactor or reducer with
constructor dependencies is default-constructed** unless you pass a real
container. Pass one, or keep observers dependency-free.

The AspNetCore package additionally registers `AddUnitOfWork()`,
`AddCompliance()`, `AddCausation()`, `AddChronicleHealthCheck()`, and
`UseCratisChronicle()` — none of which the base package gives you.

**Keep the process alive.** Observation is a live gRPC duplex stream; a console
app that appends and returns from `Main` never sees a reactor run.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Looking for `IChronicleClient.Connect` | It does not exist; `GetEventStore` connects, and `eventStore.Connection.Connect()` is the explicit form |
| `services.AddCratisChronicle(...)` | Not a real extension; it is on `IHostApplicationBuilder`/`WebApplicationBuilder` |
| `builder.WithClaimsBasedNamespaceResolver()` | Not a real extension; pass the resolver to the client instead |
| Shipping the default TLS behavior to production | Certificate validation is skipped by default |
| Constructing a client without an `ILoggerFactory` | You get a silent logger and lose every diagnostic the client emits |
| A reactor with constructor dependencies and no `IServiceProvider` | It is built by `Activator.CreateInstance` with no arguments |
| Polling `IsConnected` to know it is ready | Connected is not registered; use `WaitForRegistration` |
| Treating a quiet reactor as "no events yet" | Keepalive loss stops observers while appends keep succeeding |
| Returning from `Main` after appending | The observation stream dies with the process |
| Expecting the analyzers | `Cratis.Chronicle.CodeAnalysis` is a separate opt-in package |
| Copying a version out of the repository | It is a `1.0.0` placeholder; take it from nuget.org |

## Verify

- The package version is the one you intended, taken from nuget.org.
- The connection succeeds and no `IncompatibleServerException` is thrown — and if
  the compatibility check was skipped, you know why (a transport error is logged
  and ignored by design).
- A production connection string sets `skipTlsValidation=false`.
- An `ILoggerFactory` is supplied, and client logs are visible.
- Observers that take dependencies get a real `IServiceProvider`.
- Readiness is established with `WaitForRegistration`, not with `IsConnected`.
- The process stays alive for as long as observation is expected.
- The build is clean and the specifications pass against the verified package
  version.
