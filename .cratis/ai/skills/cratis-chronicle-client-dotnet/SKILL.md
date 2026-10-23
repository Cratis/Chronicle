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

Source: `Cratis/Chronicle`.

| Package | Purpose |
| --- | --- |
| `Cratis.Chronicle` | `ChronicleClient`, `IEventStore`, `[EventType]`, existing observers; reducer discovery and nomination (`DefaultClientArtifactsProvider.cs`, `Reducers/{SelfReducerMethods,RegisteredReducerReadModel,ReducerCatalog}.cs`, `Registrations/ExplicitArtifacts.Types.cs`) |
| `Cratis.Chronicle.Connections` | `ChronicleConnectionString`, connection lifecycle |
| `Cratis.Chronicle.AspNetCore` | ASP.NET Core wiring, header/subdomain namespace resolvers |
| `Cratis.Chronicle.Testing` | in-process scenarios |
| `Cratis.Chronicle.CodeAnalysis` | CHR analyzers, distributed as a client dependency |

Take the version from nuget.org, not the placeholder version in the source.

Read references/occurrences.md when recording durable triggers, implementing
occurrence handlers, or choosing startup/shutdown activation and retry semantics.
Read references/immediate-append-refusal.md when a unit must prevent immediate
appends from escaping its atomic receipt boundary before transport.

## Lifecycle

Read [references/lifecycle.md](references/lifecycle.md) when waiting for registration (`WaitForRegistration`, not polling `IsConnected`), diagnosing quiet observers (keepalive loss fails silently, not with an exception), or disposing the client.

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

Requires Chronicle 19.4.8 for `Cratis.Chronicle` to bring `Cratis.Chronicle.CodeAnalysis` as a
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

### Options and connection strings

Read [references/connection-options.md](references/connection-options.md) when setting `ChronicleOptions`, writing a connection string or choosing credentials. A production connection string must set `skipTlsValidation=false`; there is no `ChronicleUrl` type.

### In a host

`AddCratisChronicle` is an extension on `IHostApplicationBuilder` (and `WebApplicationBuilder`), not on `IServiceCollection`; there is no `WithClaimsBasedNamespaceResolver`.
`IServiceCollection` instead has `AddCratisChronicleClient` and
`AddCratisChronicleConnection` (including `skipTlsValidation`,
`skipCompatibilityCheck` and `skipKeepAlive` parameters).

Read [references/hosting.md](references/hosting.md) when wiring Chronicle into a worker or ASP.NET Core host, or when `AddCratisChronicle` or a namespace resolver does not resolve.

## Pin event generations for delivery

`ChronicleOptions.EventGenerationDelivery` defaults to `Compatibility`. Opt in
with `EventGenerationDelivery.Pinned` (`Cratis.Chronicle.Observation`) **before
connecting**. Reactors/reducers receive their handler's CLR event generation;
typed materialized projections use each clause's CLR generation.

Requires Chronicle 19.41.0 for reactors/reducers and 19.48.0 for typed materialized projections.

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
`Source/Clients/DotNET/Events/EventTypeAttribute.cs`:

```csharp
public sealed class EventTypeAttribute(string id = "", uint generation = EventTypeGeneration.FirstValue) : Attribute
```

**Two arguments only** — `id` (empty means the type name) and `generation`
(`EventTypeGeneration.FirstValue` is `1U`). There is no `isPublic` parameter.
`AttributeUsage` is `AttributeTargets.Class`, which records satisfy.

For a new event, pass nothing: the type name is the identifier. Use `generation`
only when evolving a contract that already exists, and prefer
`EventTypeGenerationForAttribute<TEventType>` over a second `[EventType]` — the
guidance is in the attribute's own remarks (`EventTypeAttribute.cs`).

## Appending

`IEventLog : IEventSequence` is a marker
(`Source/Clients/DotNET/EventSequences/IEventLog.cs`); the surface is on
`IEventSequence` (`Source/Clients/DotNET/EventSequences/IEventSequence.cs`):

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

Requires Chronicle kernel 19.30.0 for event source definitions.

Event source definitions: a never-instantiated type marked
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
  (`Source/Clients/DotNET/Events/EventSourceId.cs`).
- `AppendResult` carries `SequenceNumber`, `IsSuccess`,
  `HasConstraintViolations`, `HasConcurrencyViolations`, `HasErrors`,
  `ConstraintViolations`, `ConcurrencyViolation`, `Errors`, and the routing facts
  (`Source/Clients/DotNET/EventSequences/AppendResult.cs`).
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
(`Source/Clients/DotNET/EventSequences/ITransactionalEventSequence.cs`); the
results live on the unit of work.

## Observing

| Artifact | Shape | Source |
| --- | --- | --- |
| Reactor | marker `IReactor` (empty interface), `[Reactor]` optional | `Reactors/IReactor.cs`, `Reactors/ReactorAttribute.cs` |
| Self model (.NET reducer default) | public static `Reduce(event, model? current[, EventContext])`, returns model or `Task<model>` (nullable results allowed); no marker | `Reducers/SelfReducerMethods.cs` |
| Separate reducer | `IReducerFor<TReadModel>` for DI, replay instance behavior or separation | `Reducers/IReducerFor.cs` |
| Plain reducer | instance handlers infer one model; nomination required | `Reducers/RegisteredReducerReadModel.cs` |
| Projection | `IProjectionFor<TReadModel>` with `Define(IProjectionBuilderFor<TReadModel>)` | `Projections/IProjectionFor.cs` |
| Model-bound projection | attributes on the read model | `Projections/ModelBound/` |
| Read model reactor | marker `IReadModelReactor`, methods `Added`/`Modified`/`Removed` | `ReadModels/IReadModelReactor.cs` |

Read [observer handlers and reads](references/discovery.md#observer-handlers-and-reads)
for reactor dispatch, an instance-handler example and `eventStore.ReadModels` APIs.
There is no `[Observer]` attribute or `IObserver` client artifact.

## Discovery

Default scanning finds marked/interface artifacts and complete self shapes.
Plain instance reducers require nomination before store creation. Self models
need no activation or fold-time DI. See the [read-model ladder](../cratis-chronicle-read-model/SKILL.md#step-3--choose-a-projection-or-a-reducer)
and [reducer contract](../cratis-chronicle-reducer/SKILL.md) for placement,
upgrades, warnings and conflicts. Self reduction is
.NET-only, not an other-client or Stage contract.

Read [discovery and nomination](references/discovery.md) for missing artifacts,
plain-class registration or `AutoDiscoverAndRegister = false`.

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
| Assuming no CHR analyzers without an explicit reference | The client brings `Cratis.Chronicle.CodeAnalysis` transitively |
| Copying a version out of the repository | It is a placeholder; take it from nuget.org |

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
