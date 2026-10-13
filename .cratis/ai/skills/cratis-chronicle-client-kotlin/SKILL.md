---
name: cratis-chronicle-client-kotlin
description: Talk to a Chronicle server from a Kotlin or Java application with the io.cratis:chronicle client - connection strings, ChronicleClient and the Spring Boot starter, @EventType classes, suspending append, reactors and reducers dispatched by first-parameter type, model-bound and declarative projections, variants (@VariantOf/@EntersOn/@GlobalFor or variantOf()/entersOn()) for an entity with mutually exclusive lifecycle shapes, classpath artifact discovery, and the blocking API Java uses. Use when building a JVM application that appends to or observes a Chronicle event store. Do not use for the .NET, TypeScript, or Elixir clients, and do not use for Chronicle kernel or Arc work.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/SKILL.md -->

# The Chronicle client for Kotlin and Java

`io.cratis:chronicle` is a **standalone client SDK**. Your application is an
ordinary JVM application that happens to talk to a Chronicle server over gRPC.
There is no framework to inherit from and no application architecture imposed:
you construct a client, ask it for an event store, and append or observe.

## Verified product sources

Verified against `Chronicle.Kotlin` at tag **`v6.13.0`** by source symbols and
signatures. Historical line citations below are locators, not current line numbers.

| Artifact | Version | Verified from |
| --- | --- | --- |
| `io.cratis:chronicle` | `6.13.0` | `Chronicle.Kotlin` at tag `v6.13.0` |
| `io.cratis:chronicle-spring-boot-starter` | `6.13.0` | same tag, `Integrations/SpringBoot` |
| `io.cratis:chronicle-testing` | `6.13.0` | same tag, `Testing` |
| `io.cratis:chronicle-contracts` | `19.30.0` | `Source/build.gradle.kts` (`chronicleContractsVersion`) |

All three artifacts are published to Maven Central under `io.cratis` (their
`build.gradle.kts` publishing blocks). The JVM toolchain is **17**.

> **Do not copy a version number out of the repository's README or docs.** At
> `v6.13.0` the README and getting-started documentation still say `6.5.0`.
> The version comes from a Gradle property injected at release time —
> `Source/build.gradle.kts` reads
> `providers.gradleProperty("version")` and defaults to `0.0.0-SNAPSHOT` — so
> the checked-in source never carries the real number. Take it from Maven
> Central.

> **The client and the kernel version independently.** Client `6.13.0` requires
> kernel `19.30.0` or later. Historically, client `6.5.0` was verified on kernel
> `19.6.1`, while `19.4.8` rejects it. `CompatibilityPreflight` checks contracts
> at connect and throws `ChronicleServerIncompatible`; do not infer a supported
> pair just because both products are "latest".

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Naming a reactor method after the event | The name is ignored; **the first parameter's type** is the subscription (`EventHandlerMethod.kt:61-74`) |
| Two `@EventType` classes sharing a simple name | The id defaults to the simple name, so they collide on the wire (`EventTypesService.kt:67`) |
| Shipping the default connection string to production | `skipTlsValidation` defaults to **true**; certificate validation is off (`ChronicleConnectionString.kt:28`) |
| Copying a version from the README or docs | Both still say `6.5.0` at `v6.13.0`; use the release version |
| Supplying `apiKey` in the connection string | Client construction rejects it with `IllegalArgumentException`; use `user:password@` |
| Expecting `getEventStore` to suspend | It does not — the client already connected in its constructor (`ChronicleClient.kt:12`) |
| Expecting `awaitRegistration()` to prove all artifacts registered | The initial gate completes in `finally` even after failure; connection rejection still throws `ChronicleConnectionFailed` |
| Treating automatic and explicit registration alike | The automatic pass logs failures; explicit `registerAll()` throws `ArtifactRegistrationFailed` for failed observers, and rejected event-type registration throws `ChronicleCommandRejected` |
| Expecting read model reactors to be discovered | `IReadModelReactor` is absent from the scan and from `IEventStore`; construct `ReadModelReactors(...)` yourself |
| Leaving the default classpath scan on in a large app | Use `withArtifactsFrom(...)` or `artifact-packages` to narrow it (`ChronicleOptions.kt:58`) |
| Expecting WebFlux support from the starter | The web auto-configuration is `SERVLET`-only (`ChronicleWebAutoConfiguration.kt:29`) |
| Exiting `main` after an append | Reactors and reducers stop with the process; the stream is live |
| Passing a `KProperty1` as `@VariantOf`'s `key` | The annotation form always takes a plain string; only the declarative `variantOf(...)` builder accepts a property reference |
| A `@GlobalFor` mapping targeting a property one variant lacks | `GlobalHandlerPropertyNotOnVariant` at registration, not a silently skipped mapping |

## Two ways in — pick one

| You are building | Use | Artifact |
| --- | --- | --- |
| A plain Kotlin/Java application, a CLI, a worker | `ChronicleClient` directly | `io.cratis:chronicle` |
| A Spring Boot application | the starter, and inject the beans | `io.cratis:chronicle-spring-boot-starter` |

The starter is a wrapper over the same client. Everything below about event
types, appending, and observers is identical either way; only the wiring differs.

## Connecting

### Plain Kotlin

```kotlin
import io.cratis.chronicle.ChronicleClient
import io.cratis.chronicle.ChronicleOptions
import kotlinx.coroutines.runBlocking

fun main() = runBlocking {
    val options = ChronicleOptions.development()
    val client = ChronicleClient(options)
    try {
        val store = client.getEventStore("<EventStoreName>")
        // ... use store ...
    } finally {
        client.dispose()
    }
}
```

**The client connects in its constructor.** `getEventStore(name, namespace)`
is not suspending and returns the cached concrete `EventStore` for that pair.
The default namespace is `"Default"`. `getEventStores()` suspends;
`evictEventStores()` drops the cache without closing the client.
`IChronicleClient` is `AutoCloseable`; `close()` delegates to `dispose()`.

### Options

Use `ChronicleOptions.development()` locally or
`ChronicleOptions.fromConnectionString(String)` for a configured connection.
Narrow discovery with `withArtifactsFrom(vararg packages)` in a large app.
Read [references/options.md](references/options.md) when setting naming policy,
registration, artifact activation, sink type or OpenTelemetry options.

### The connection string

`ChronicleConnectionString.kt` documents the grammar at `:14-18`:

```
chronicle://<host>[:<port>][,<host>[:<port>]...][?<options>]
chronicle://<username>:<password>@<host>[:<port>][,...][?<options>]
chronicle+srv://<host>[:<port>][?<options>]
```

Default port is `35000` (`:39`). Recognized query keys, lowercased at parse time
(`ChronicleConnectionString` parser): `disabletls`, `skiptlsvalidation`,
`loadbalancer`, `srvnameserver`. `apiKey` is parsed but rejected at client
creation with `IllegalArgumentException`: the kernel has no API-key authentication.
Use `user:password@`. Omitting credentials selects `chronicle-dev-client` /
`chronicle-dev-secret`; these are development credentials, not a production setup. `ChronicleConnectionString.parse(String)` is on the companion
(`:63`), and `ChronicleConnectionString.DEVELOPMENT` (`:51`) points at
`localhost:35000` with the `chronicle-dev-client` / `chronicle-dev-secret`
credentials (`:42-43`).

> **TLS is on, certificate validation is off by default** (`disableTls = false`,
> `skipTlsValidation = true`), for a self-signed development kernel.
> **Production must set `?skipTlsValidation=false`** for platform trust-store
> chain validation (`ChronicleConnectionString.createCredentials`).

### Spring Boot

The starter binds `cratis.chronicle.*` (minimum: `event-store`) and exposes the `IChronicleClient`, `IEventStore` and `Chronicle` beans; its web auto-configuration is servlet-only.

Read [references/spring-boot.md](references/spring-boot.md) when wiring or configuring the Spring Boot starter (properties, namespace resolution, injected beans, WebFlux).

## Defining event types

```kotlin
import io.cratis.chronicle.events.EventType

/** <What happened, in the past tense.> */
@EventType
data class <EventName>(
    val <property>: <Type> = <default>
)
```

`io.cratis.chronicle.events.EventType` — `events/EventType.kt:15-19`:

```kotlin
annotation class EventType(
    val id: String = "",
    val generation: Int = 1,
    val tombstone: Boolean = false
)
```

**The id defaults to the class's *simple* name, not its fully qualified name** —
`events/EventTypesService.kt:67` resolves `ann.id.ifEmpty { cls.simpleName!! }`.
Two event classes with the same simple name in different packages therefore
collide on the wire. Give one an explicit `id`.

The house shape is a Kotlin `data class` with defaulted properties or a Java
`record` (the Spring Boot samples). Property-level
annotations that travel with the event: `@io.cratis.chronicle.keys.Key`,
`@io.cratis.chronicle.compliance.Pii`, `@io.cratis.chronicle.Subject`,
`@io.cratis.chronicle.schemas.JsonSchemaType`.

`@Pii` on a `ConceptAs` type is declared once and cascades to its uses.
For operational secrets (API keys, partner credentials), use
`@io.cratis.chronicle.confidentiality.Encrypted(scope = EncryptionScope.Subject)`;
`EncryptionScope` also offers `Namespace` and `Global`. Encryption at rest is
not right-to-erasure: use `@Pii` for personal data. A value cannot combine
`@Pii` and `@Encrypted`, and neither is supported on an `EventSourceId` concept.

Evolving a schema is `IEventTypeMigration<TTarget, TSource>` with `upcast` and
`downcast` — `events/migrations/IEventTypeMigration.kt:18-37`. Migrations
register in the same call as event types
(`artifacts/ArtifactRegistrations.kt:63`).

## Appending

Everything that touches the kernel suspends. `IEventSequence.kt`:

```kotlin
suspend fun append(eventSourceId: String, event: Any, options: AppendOptions? = null): AppendResult          // :37
suspend fun appendMany(eventSourceId: String, events: List<Any>, options: AppendOptions? = null): List<AppendResult>  // :47
suspend fun appendMany(                                                                                       // :65
    events: List<EventForEventSourceId>,
    concurrencyScopes: Map<String, ConcurrencyScope> = emptyMap(),
    correlationId: UUID? = null
): List<AppendResult>
```

```kotlin
val result = store.eventLog.append("<event-source-id>", <EventName>(<value>))
if (!result.isSuccess) {
    // result.constraintViolations, result.concurrencyViolation, result.errors
}
```

- **The event source id is a plain `String`** at every call site
  (`IEventSequence.kt:37`). Typed alternatives taking `ConceptAs<String>` exist
  as extension functions in `io.cratis.chronicle.concepts`
  (`concepts/EventSourceIdConcepts.kt`).
- `AppendResult` carries `sequenceNumber`, `constraintViolations`, `errors`,
  `isSuccess`, `concurrencyViolation` — `eventSequences/AppendResult.kt:18-33`.
  It also carries `sequenceNumberValue: Long` (`:32`) purely so Java can read
  the number, because `EventSequenceNumber` is a `@JvmInline value class` whose
  getter Java cannot name.
- The three-argument `appendMany(List<EventForEventSourceId>, ...)` is the only
  overload that commits atomically **across** event sources.
- Reach for the second overload's `EventForEventSourceId` when events in one
  batch must go to different streams — the single-source overloads cannot
  express that.

`AppendOptions` (`eventSequences/AppendOptions.kt`) carries `correlationId`,
`concurrencyScope`, `eventSourceType`, `eventStreamType`, `eventStreamId`,
`subject`, `tags`, `occurred`, `causation`, `eventSource`, `eventStream`.

Since `6.0.0`, omitted/empty routing resolves **in the kernel** to
`Default` / `All` / `Default` (source type / stream type / stream id).
The client no longer substitutes the event source id as stream id. Preserve a
legacy route by passing all three explicitly. Reads narrow only by supplied
filters; single-source `appendMany` applies the routing options to every event.
See the product's `Documentation/guides/migrate-append-routing.md` at `v6.13.0`.

For an idempotent create, narrow the concurrency scope and use
`ConcurrencyScopeBuilder.withExpectsNoMatchingEvent()`. The equivalent is
`ConcurrencyScope(EventSequenceNumber.unavailable, eventSourceId = true,
expectsNoMatchingEvent = true)`; a concrete sequence number and this flag are
mutually exclusive (package `eventSequences.concurrency`).

Event-source definitions and their named streams are optional, discovered
artifacts requiring kernel `19.30.0` or later.
Read [references/event-sources.md](references/event-sources.md) when declaring
`@EventSource`/`@EventStream` or appending through a definition.

A unit of work spans several appends:

```kotlin
val unitOfWork = store.unitOfWorkManager.begin()
store.eventLog.transactional.append("<id>", <EventName>(<value>))
store.eventLog.transactional.appendMany("<id>", listOf(<OtherEvent>()))
unitOfWork.commit()
```

In a unit of work, `transactional` appends return a placeholder `AppendResult`
(sequence `-1`); real results are on the unit of work after `commit()`.

## Observing

### Handlers are found by their first parameter's type

This is the single most important convention in this client, and it is not the
method name. `observation/EventHandlerMethod.kt:61-74` reads a function as a
handler only when **parameter index 1 — the first real parameter — is a class
annotated with `@EventType`**. The method name is irrelevant. A handler may be
`suspend` or plain; both are invoked through `callSuspend`.

```kotlin
import io.cratis.chronicle.events.EventContext
import io.cratis.chronicle.observation.Reactor

@Reactor
class <ReactorName>(private val <dependency>: <Dependency>) {
    fun <anyMethodName>(event: <EventName>, context: EventContext): <SideEffectEvent> {
        <dependency>.<doSomething>(event.<property>)
        return <SideEffectEvent>(<property> = context.eventSourceId)
    }
}
```

**A returned event is appended as a side effect.** `null`, `Unit`, and any value
whose type is not annotated `@EventType` are ignored; a single event, an
`EventForEventSourceId`, or a `List` mixing both is appended, defaulting to the
triggering event source. A returned `List` is one atomic batch: one rejected
event rejects all — `observation/ReactorSideEffects.kt:26-39`, `:48-59`.
This is how a reactor appends without ever touching the event log.

`EventContext` is a data class with `sequenceNumber: Long`, `eventSourceId`,
`eventType`, `occurred`, `correlationId`, `causedBy`, `eventSourceType`,
`eventStreamType`, `eventStreamId`, `eventStore`, `namespace`, `causation`,
`tags`, `hash`, `observationState`, `subject`, `eventSource` —
`events/EventContext.kt`. The constructor changed in `6.0.0`; recompile on upgrade.

### The annotations

| Annotation | Arguments | Source |
| --- | --- | --- |
| `@Reactor` | `id = ""`, `eventSequence = ""` | `observation/Reactor.kt:15` |
| `@Reducer` | `id = ""`, `eventSequence = ""`, `isActive = true` | `observation/Reducer.kt:17` |
| `@ReadModel` | `id = ""`, `displayName = ""` | `readModels/ReadModel.kt:19` |
| `@Projection` | `id = ""`, `eventSequence = ""` — optional | `projections/Projection.kt:22` |
| `@Constraint` | `id = ""` | `constraints/Constraint.kt:13` |
| `@Seeder` | none | `seeding/Seeder.kt:11` |

Handler-level: `@Replay`, `@OnceOnly`. Observer filtering: `@Tag`/`@Tags`,
`@FilterEventsByTag`/`@FilterEventsByTags`, `@EventSequence`,
`@EventSourceType`, `@EventStreamType`.

A reducer's handler may be `(event)`, `(event, state)`, or
`(event, state, context)` — the three shapes accepted by the dispatcher and by
`ReadModelScenario` (`Testing/.../ReadModelScenario.kt:146-150`). **Reducers run
client-side**: the kernel streams events and the handler is invoked in your
process (`observation/ReducersService.kt:119-140`).

### Read models and model-bound projections

Read models are queried through `store.readModels`
(`readModels/IReadModelsService.kt`): `getInstanceByKey(readModelClass, key): T?`
(`:11`), `getInstances(readModelClass, eventCount)` (`:20`),
`getSnapshotsById` (`:29`), and `watch(readModelClass): Flow<ReadModelChangeset<T>>`
(`:37`, which is **not** suspending — it hands back a `Flow`).

A model-bound projection puts the projection on the read model with `@FromEvent`
(`projections/FromEvent.kt:17`) and `@SetFrom`
(`projections/SetFrom.kt:22`); the full family also includes `SetValue`,
`SetFromContext`, `AddFrom`, `SubtractFrom`, `Increment`, `Decrement`, `Count`,
`Join`, `RemovedWith`, `RemovedWithJoin`, `ChildrenFrom`, `ClearWith`,
`FromAll`, `FromEvery`, `FromEventSourceId`, `CountFromAll`, `IncrementFromAll`,
`DecrementFromAll`, `Nested`, `NoAutoMap`, `NoAutoMapProperties`,
`NotRewindable`. The declarative
alternative is a class implementing `IProjectionFor<TReadModel>`.
Use fluent `set(...).toValue(...)` for a constant, or `toProperty(...)` for an
event property. `set(...).to { }` is deprecated: it never evaluated the lambda.
`noAutoMap()` is honored; composite key parts can use
`toEventContextProperty(targetPropertyName, contextProperty)`.

`@io.cratis.chronicle.readModels.Passive` and `@Reducer(isActive = false)`
compute a passive model from its events on read, so a read immediately after an
append includes that event rather than waiting for a materialized observer.

### Variants — mutually exclusive read models for one entity's lifecycle

Variants let several read models share one identity and be mutually exclusive; each variant must declare an `@EntersOn`/`entersOn(...)` event or registration throws `VariantMustDeclareEntersOnEvent`. Annotations require `6.3.0`; use `6.5.0` or later for end-to-end key support and replay once when upgrading from an earlier version.

Read [references/variants.md](references/variants.md) when an entity changes shape over its lifecycle and you need `@VariantOf`, `@EntersOn`, `@GlobalFor` or the declarative `variantOf`/`entersOn` builder.

## Discovery and registration

Artifacts are found by a classpath scan and registered in a fixed order, re-run on every reconnect.

Read [references/discovery-and-registration.md](references/discovery-and-registration.md) when you need the scan rules, registration order, reconnect behavior, or `registerAll()` with `autoDiscoverAndRegister = false`.

## Java

Java code starts at `BlockingChronicleClient.connect(...)`, not the raw `ChronicleClient` plus `*JavaBridge` statics; in Spring Boot, inject the `Chronicle` bean.

Read [references/java-api.md](references/java-api.md) when writing Java against the client (blocking surface, conformance suite, README's older bridge route).

## Connection lifecycle

Keepalive is two-way and silence, not an error, is how the connection dies; your process must stay alive for reactors and reducers to keep receiving.

Read [references/connection-lifecycle.md](references/connection-lifecycle.md) when diagnosing disconnects, reconnect/backoff behavior, keepalive timeouts or shutdown.

## Testing

`io.cratis:chronicle-testing` runs in-process with no kernel and no Docker:
`EventScenario` (`Testing/.../EventScenario.kt:35`) and
`ReadModelScenario<TReadModel>` (`Testing/.../ReadModelScenario.kt:47`), which
folds a reducer through the same handler-shape rules as production
(`ReadModelScenario.kt:139-150`). Its scope is small — appends and reducer folds
only; there is no in-process reactor, projection, or constraint scenario
(`Testing/api/Testing.api`). Since `6.5.0`, `EventScenario` assertions and
`eventsOf` count only events appended after the last `given`.

## Verify

- The dependency resolves from Maven Central and the version is the one you
  intended — not a number copied from a README.
- A connect against the target kernel succeeds, and the client/server version
  pair is one you confirmed rather than assumed.
- A production connection string sets `skipTlsValidation=false`.
- Every `@EventType` class has a unique simple name, or an explicit `id`.
- Every reactor and reducer handler's **first parameter** is an `@EventType`
  class.
- Handle `ChronicleConnectionFailed` from `awaitRegistration()`/appends and
  Spring startup; inspect automatic registration diagnostics, or handle explicit
  `registerAll()` failures. The initial gate alone is not proof of full registration.
- Every variant group has at least one `@EntersOn`/`entersOn(...)` per variant,
  and every `@GlobalFor` member exists on every variant it targets.
- The build is clean and the specifications pass against the verified artifact
  version.
