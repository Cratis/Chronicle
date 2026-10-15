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

Verified against `Cratis/Chronicle` **v19.50.1**. The client surface is unchanged
from v19.50.0; earlier release numbers identify feature availability.

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `19.50.1` | `ChronicleClient`, `IEventStore`, `[EventType]`, observers |
| `Cratis.Chronicle.Connections` | `19.50.1` | `ChronicleConnectionString`, connection lifecycle |
| `Cratis.Chronicle.AspNetCore` | `19.50.1` | ASP.NET Core wiring, header/subdomain namespace resolvers |
| `Cratis.Chronicle.Testing` | `19.50.1` | in-process scenarios |
| `Cratis.Chronicle.CodeAnalysis` | `19.50.1` | CHR analyzers, distributed as a client dependency |

Take the version from nuget.org, not the `1.0.0` source pack-time placeholder.
Reverify before claiming another version.

## Lifecycle

- **Registration is wired to the connection, not called by you.**
  `EventStore.RegisterAll` subscribes to `Connection.Lifecycle.OnConnected`
  when `autoDiscoverAndRegister` is on; reconnect re-registers artifacts.
- `DiscoverAll()` does event types first, then constraints, reactors, reducers,
  projections and seeding in parallel; registration is single-flighted with
  jittered backoff/background retry (`EventStore.cs`).
- **Wait with `WaitForRegistration`, not by polling `IsConnected`.**
  `RegistrationWaitExtensions.WaitForRegistration(this IEventStore, TimeSpan? timeout = default)`
  returns `RegistrationOutcome`, including failed runs; its default 5-second
  timeout is for a run that never finished. Inspect `IsSuccess`, `Failure`,
  `Failures` and `IdentityDrift`. It reports projection registrations, not
  reactor/reducer subscription readiness (use `WaitTillSubscribed` / `WaitTillActive`).
  Successful active projection registration includes observer subscription, not
  read-your-writes: inspect the append's `WaitForCompletion` result or read a
  passive model. Source: `Registrations/RegistrationOutcome.cs`.
- **Keepalive is a bidirectional stream plus a watchdog.** The watchdog monitors
  every `MonitorIntervalMilliseconds = 1000`
  (`Source/Clients/Connections/ConnectionWatchdog.cs`) and a keepalive that
  falls more than 5 seconds behind is treated as a lost connection, with
  reconnect backoff capped at 30 seconds. **The failure mode is silence, not an
  exception** — appends keep working while observers go quiet.
- `Dispose()` disposes read model reactors for created stores, cancels the owned
  connection, and disposes the connection
  (`Source/Clients/DotNET/ChronicleClient.cs`, `Dispose`).

## What a standalone app owns that a host would have supplied

Every one of these has a silent default. Read them as a checklist, because the
defaults are reasonable for a sample and wrong for a service
(`ChronicleClient.cs`, options constructor):

| Concern | Default when you pass nothing |
| --- | --- |
| `IClientArtifactsProvider` | `DefaultClientArtifactsProvider.Default` — full assembly scan |
| `IServiceProvider` | `DefaultServiceProvider`, which activates everything through `Activator.CreateInstance` |
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

## Which package

```shell
dotnet add package Cratis.Chronicle              # console / worker service
dotnet add package Cratis.Chronicle.AspNetCore   # ASP.NET Core
```

Since 19.4.8, `Cratis.Chronicle` brings `Cratis.Chronicle.CodeAnalysis` as a
package dependency: consumers receive the CHR analyzers without opting in.
The package id is still separate; Chronicle does not run these analyzers over
its own generic client plumbing. Source: `Source/Clients/DotNET/DotNET.csproj`.

## Connecting

### The smallest real thing

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

using var client = new ChronicleClient(ChronicleConnectionString.Development);
var eventStore = await client.GetEventStore("<EventStoreName>");
```

Source: `Documentation/client-snippets/get-started/console/connect.md`;
client snippets compile in product CI. Prefer them to untested prose examples.

**There is no `IChronicleClient.Connect`.** `GetEventStore` discovers and connects;
`eventStore.Connection.Connect()` is the explicit connection form.
`ChronicleClient` is `IDisposable`; supply the options constructor's collaborators
instead of retaining sample defaults. Read [client construction](references/client-construction.md)
when choosing the constructor or passing a real container/logger.

### Options

`ChronicleOptions` — `Source/Clients/DotNET/ChronicleOptions.cs`. Statics:
`FromConnectionString(string)`, `FromConnectionString(ChronicleConnectionString)`,
`FromDevelopmentConnectionString()`. The parameterless constructor uses the
development connection string.

Options worth knowing: `AutoDiscoverAndRegister` (default `true`),
`ConnectTimeout` (5 seconds), `SkipKeepAlive` (short-lived clients),
`DefaultSinkTypeId` (`WellKnownSinkTypes.MongoDB`), `Tls`, `Authentication`,
`RegistrationRetry`, `SkipCompatibilityCheck`, `ArtifactIdentityDrift` (`Warn`),
`UnitOfWorkLifecyclePolicy` (`Compatibility`; `Strict` opts in to completion
checks), `EventGenerationDelivery`, `ExplicitArtifacts`, `ConcurrencyOptions`,
`ClaimsBasedNamespaceResolverClaimType` (`tenant_id`), and
`MaxReceiveMessageSize` / `MaxSendMessageSize` (100 MB each).
`EnableEventTypeGenerationValidation` opts into strict migration-chain checking
on development kernels; production always enforces it.

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

Default port `35000`. `Default` is `chronicle://localhost:35000`;
`Development` adds the `chronicle-dev-client` / `chronicle-dev-secret`
credentials. Query options: `apiKey`, `auth`, `skipTlsValidation`,
`skipCompatibilityCheck`, `loadBalancer`, `srvNameServer`, `certificatePath`,
`certificatePassword` (`ChronicleConnectionStringBuilder.cs`). There is an
implicit conversion from `string` and a `Redacted` form for logging.

With ClientCredentials auth and **both** username and password absent, the .NET
client also falls back to these development credentials, including through
`AddCratisChronicleConnection`. Only a `DEVELOPMENT`-compiled kernel creates the
application, directly in persistent storage. Switching that storage to a
production image does not remove it: remove the development application and any
configuration declaration that would recreate it. Do not infer other clients'
authentication behavior from this one. Verified at **v19.50.0**:
`Source/Clients/DotNET/ChronicleClient.cs:334-345`,
`Source/Clients/Connections/ServiceCollectionExtensions.cs:57-68`, and
`Source/Kernel/Core/Setup/Authentication/AuthenticationService.cs:170-208`.
Read [host wiring](references/hosting.md) for the deployment distinction.

> **Certificate validation is skipped by default, and that is deliberate.** The
> development Chronicle images (`latest-development` and `latest-development-slim`)
> generate a self-signed certificate on every start, so a development pair
> connects with no setup. The production image does not: it refuses to start with
> TLS enabled (the default) and no configured certificate. **A production connection string must set
> `skipTlsValidation=false`** (or `Tls.SkipCertificateValidation = false`)
> against a server whose certificate is actually verifiable.

### In a host

`AddCratisChronicle` is an extension on `IHostApplicationBuilder` (and `WebApplicationBuilder`), not on `IServiceCollection`; there is no `WithClaimsBasedNamespaceResolver`.
`IServiceCollection` instead has `AddCratisChronicleClient` and
`AddCratisChronicleConnection` (including `skipTlsValidation`,
`skipCompatibilityCheck` and `skipKeepAlive` parameters).

Read [references/hosting.md](references/hosting.md) when wiring Chronicle into a worker or ASP.NET Core host, or when `AddCratisChronicle` or a namespace resolver does not resolve.

## Pin event generations for delivery

`ChronicleOptions.EventGenerationDelivery` defaults to `Compatibility`. Opt in
with `EventGenerationDelivery.Pinned` (`Cratis.Chronicle.Observation`) **before
connecting**. Reactors/reducers receive their handler's CLR event generation
(**19.41.0+**); typed materialized projections use each clause's CLR generation
(**19.48.0+**), reverified at **19.50.0**.

Read [references/pinned-generation-delivery.md](references/pinned-generation-delivery.md)
for compiling standalone/host configuration, registration constraints, and the
Compatibility-only surfaces. For migration, PDL pin persistence, and integration
test requirements, use **cratis-chronicle-event-type-migration**.

## The client and the server check each other

The client runs the server-side compatibility check automatically inside `Connect()`; a genuine mismatch throws `IncompatibleServerException`, while a transport error is logged and ignored by design.

Read [references/compatibility-check.md](references/compatibility-check.md) when diagnosing an `IncompatibleServerException`, a skipped compatibility check, or an old-server fallback.

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

Event source definitions (Chronicle 19.30.0 and later): a never-instantiated type marked
`IEventSource`, named by `[EventSource(name)]` (default: the type name without a trailing
`EventSource`) and declaring streams with `[EventStream(name)]` (all in
`Cratis.Chronicle.EventSources`; both take `Description` and `Concurrency`). Append through
it with `EventForEventSourceId { EventSource = typeof(<Definition>), EventStream = "<stream>" }`;
the source and stream types come from the definition and contradicting explicit
`EventSourceType`/`EventStreamType` values are rejected. The stream id stays with the caller
(`EventStreamId`, or `ICanProvideEventStreamId.GetEventStreamId()` on a reactor or command).

```csharp
[EventSource("Account")]
[EventStream("Transactions")]
public class Account : IEventSource;

await eventStore.EventLog.AppendMany(
[
    new EventForEventSourceId(<accountId>, new <Deposited>(<amount>))
    {
        EventSource = typeof(Account),
        EventStream = "Transactions",
        EventStreamId = <streamId>
    }
]);
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

Reading back is on `IEventSequence`: `GetForEventSourceIdAndEventTypes`,
`HasEventsFor`, `GetFromSequenceNumber`, `GetNextSequenceNumber`, and
`GetTailSequenceNumber`. Redaction is `Redact`; subject erasure is a separate
compliance operation.

Read [additional client surfaces](references/additional-surfaces.md) when using
named tags, prepared appends, metadata-only reads, seeding or cross-cutting event
properties. For `CompleteStream`, use **cratis-chronicle-event-constraints**;
for guarded state-dependent decisions, use **cratis-chronicle-decision-reads**.

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

Artifacts are found by assembly scanning, with no registration call and no DI container required.

Read [references/discovery.md](references/discovery.md) when an event type, reactor, reducer or projection is not being found, or when you need explicit registration with `AutoDiscoverAndRegister = false`.

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
| Assuming no CHR analyzers without an explicit reference | The client brings `Cratis.Chronicle.CodeAnalysis` transitively since 19.4.8 |
| Copying a version out of the repository | It is a `1.0.0` placeholder; take it from nuget.org |

## Verify

- The package version is the one you intended, taken from nuget.org.
- The connection succeeds and no `IncompatibleServerException` is thrown — and if
  the compatibility check was skipped, you know why (a transport error is logged
  and ignored by design).
- A production connection string sets `skipTlsValidation=false`.
- An `ILoggerFactory` is supplied, and client logs are visible.
- Observers that take dependencies get a real `IServiceProvider`.
- Projection readiness inspects the `WaitForRegistration` outcome, not `IsConnected`;
  reactor/reducer readiness waits for their observer state.
- CHR0054–CHR0057 are clean: dispatchable returns, valid typed stream filters,
  and one projection/reducer target per class.
- The process stays alive for as long as observation is expected.
- The build is clean and the specifications pass against the verified package
  version.
