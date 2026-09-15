---
name: cratis-chronicle-client-kotlin
description: Talk to a Chronicle server from a Kotlin or Java application with the io.cratis:chronicle client - connection strings, ChronicleClient and the Spring Boot starter, @EventType classes, suspending append, reactors and reducers dispatched by first-parameter type, model-bound read models, classpath artifact discovery, and the blocking API Java uses. Use when building a JVM application that appends to or observes a Chronicle event store. Do not use for the .NET, TypeScript, or Elixir clients, and do not use for Chronicle kernel or Arc work.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/SKILL.md -->

# The Chronicle client for Kotlin and Java

`io.cratis:chronicle` is a **standalone client SDK**. Your application is an
ordinary JVM application that happens to talk to a Chronicle server over gRPC.
There is no framework to inherit from and no application architecture imposed:
you construct a client, ask it for an event store, and append or observe.

## Verified product sources

This skill is verified against this exact source:

| Artifact | Version | Verified from |
| --- | --- | --- |
| `io.cratis:chronicle` | `4.0.0` | `Chronicle.Kotlin` at tag `v4.0.0` (commit `63ff629`) |
| `io.cratis:chronicle-spring-boot-starter` | `4.0.0` | same tag, `Integrations/SpringBoot` |
| `io.cratis:chronicle-testing` | `4.0.0` | same tag, `Testing` |
| `io.cratis:chronicle-contracts` | `16.44.1` | `Source/build.gradle.kts:11` |

All three artifacts are published to Maven Central under `io.cratis`
(`Source/build.gradle.kts:54`, `Testing/build.gradle.kts:32`,
`Integrations/SpringBoot/build.gradle.kts:48`). The JVM toolchain is **17**
(`Source/build.gradle.kts:43`).

> **Do not copy a version number out of the repository's README or docs.** At
> `v4.0.0` the README still says `2.1.1` (`README.md:186`) and the documentation
> says `2.1.2` (`Documentation/get-started/index.md:24`). The version comes from
> a Gradle property injected at release time — `Source/build.gradle.kts:8` reads
> `providers.gradleProperty("version")` and defaults to `0.0.0-SNAPSHOT` — so
> the checked-in source never carries the real number. Take it from Maven
> Central.

> **The client and the kernel version independently.** Client `4.0.0` is built
> against `chronicle-contracts` `16.44.1`; the Chronicle server has since moved
> to a `17.x` line. Confirm the client/server pair you intend to run is
> supported before relying on it — do not infer compatibility from the fact that
> both are "latest".

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

`IChronicleClient` is small and complete —
`Source/src/main/kotlin/io/cratis/chronicle/IChronicleClient.kt`:

| Member | Line | Note |
| --- | --- | --- |
| `fun getEventStore(name: String, namespace: String = EventStoreNamespaceName.default.value): EventStore` | `:17-20` | **not** suspending; returns the concrete `EventStore` |
| `suspend fun getEventStores(): List<String>` | `:27` | asks the kernel |
| `fun evictEventStores()` | `:36` | drops the local cache, keeps the client |
| `fun dispose()` | `:39` | `IChronicleClient : AutoCloseable`, `close()` delegates to it (`:41`) |

**The client connects in its constructor.** `ChronicleClient.kt:13` is
`ChronicleConnection(options.connectionString).also { it.connect() }` — there is
no separate `connect()` step to call, and constructing the client is the
connecting act. Event stores are cached per `"$name/$namespace"`
(`ChronicleClient.kt:22`), so repeated `getEventStore` calls return the same
instance.

The default namespace is the literal `"Default"` —
`EventStoreNamespaceName.kt:13`.

### Options

`ChronicleOptions` is a `data class` with `@JvmOverloads`
(`ChronicleOptions.kt:35`):

| Property | Default | Line |
| --- | --- | --- |
| `connectionString` | required | `:36` |
| `programIdentifier` | `"Unknown"` | `:37` |
| `defaultSinkTypeId` | `CHRONICLE_SINK_TYPE` env var, else `WellKnownSinkTypes.MONGODB` | `:38` |
| `autoDiscoverAndRegister` | `true` | `:39` |
| `artifacts` | `ClientArtifacts.default` (scans the classpath) | `:40` |
| `artifactActivator` | `ArtifactActivator` | `:41` |
| `openTelemetry` | `null` (uses the globally registered one) | `:42` |

Two `@JvmStatic` factories: `ChronicleOptions.fromConnectionString(String)`
(`:69`) and `ChronicleOptions.development()` (`:78`). Two instance helpers:
`withoutAutoRegistration()` (`:48`) and `withArtifactsFrom(vararg packages)`
(`:58`) — the second is worth using in any large application, because the
default scan walks the whole classpath.

### The connection string

`ChronicleConnectionString.kt` documents the grammar at `:14-18`:

```
chronicle://<host>[:<port>][,<host>[:<port>]...][?<options>]
chronicle://<username>:<password>@<host>[:<port>][,...][?<options>]
chronicle+srv://<host>[:<port>][?<options>]
```

Default port is `35000` (`:39`). Recognized query keys, lowercased at parse time
(`:111-115`): `disabletls`, `skiptlsvalidation`, `apikey`, `loadbalancer`,
`srvnameserver`. `ChronicleConnectionString.parse(String)` is on the companion
(`:63`), and `ChronicleConnectionString.DEVELOPMENT` (`:51`) points at
`localhost:35000` with the `chronicle-dev-client` / `chronicle-dev-secret`
credentials (`:42-43`).

> **TLS is on, certificate validation is off, by default.** `disableTls = false`
> but `skipTlsValidation = true` — `ChronicleConnectionString.kt:27-28`, and the
> credential selection at `:180-181` installs an `InsecureTrustManager` in that
> case. That default exists because a development kernel serves a self-signed
> certificate. **A production connection string must carry
> `?skipTlsValidation=false`**, which is the only way to get chain validation
> against the platform trust store (`:174-176`).

### Spring Boot

The starter registers exactly two auto-configurations —
`Integrations/SpringBoot/src/main/resources/META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports`:

- `io.cratis.chronicle.spring.ChronicleAutoConfiguration`
- `io.cratis.chronicle.spring.ChronicleWebAutoConfiguration`

Configuration binds under the prefix `cratis.chronicle` —
`ChronicleProperties.kt:42-43`. The minimum real configuration is one key, as in
the shipped sample (`Samples/Kotlin/SpringBoot/src/main/resources/application.yml`):

```yaml
cratis:
  chronicle:
    event-store: <EventStoreName>
```

Everything else has a default (`ChronicleProperties.kt:44-55`):

| Key under `cratis.chronicle` | Default | Line |
| --- | --- | --- |
| `connection-string` | the development connection string | `:44` |
| `event-store` | `"Default"` | `:45` |
| `namespace` | `"Default"` | `:46` |
| `auto-discover-and-register` | `true` | `:47` |
| `artifact-packages` | empty — falls back to Spring's auto-configuration packages | `:48` |
| `default-sink-type-id` | `null` | `:49` |
| `program-identifier` | `null` — falls back to `spring.application.name` | `:50` |
| `registration-timeout` | `PT30S` | `:51` |
| `namespace-resolution.strategy` | `FIXED` | `:65` |
| `namespace-resolution.http-header` | `"x-cratis-tenant-id"` | `:66` |
| `namespace-resolution.claim` | `"tenant_id"` | `:67` |

`NamespaceResolution.Strategy` is `FIXED`, `HTTP_HEADER`, `SUBDOMAIN`,
`AUTHENTICATION` (`ChronicleProperties.kt:70-82`).

The beans you inject are `IChronicleClient`, `IEventStore`, and the convenience
facade `Chronicle` (`ChronicleAutoConfiguration.kt:93-114`). `Chronicle` wraps
the suspending API in blocking calls and takes `Class<T>` rather than
`KClass<T>`, so Java can use it unchanged —
`Integrations/SpringBoot/src/main/kotlin/io/cratis/chronicle/spring/Chronicle.kt:47`,
`append` at `:57`, `appendMany` at `:69`.

`ChronicleWebAutoConfiguration` is **servlet-only**
(`@ConditionalOnWebApplication(SERVLET)`, `ChronicleWebAutoConfiguration.kt:29`).
A WebFlux application gets no per-request namespace, identity, causation, or
unit-of-work filter.

**Artifacts are Spring beans.** `SpringArtifactActivator` resolves a discovered
artifact from the container when it is uniquely defined and otherwise builds it
through `autowireCapableBeanFactory.createBean`
(`SpringArtifactActivator.kt:28-43`), so a reactor takes constructor
dependencies exactly like a `@Service` would.

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

The house shape is a Kotlin `data class` with defaulted properties
(`Samples/Kotlin/SpringBoot/.../Events.kt:9-14`) or a Java `record`
(`Samples/Java/SpringBoot/.../EmployeeHired.java:9-10`). Property-level
annotations that travel with the event: `@io.cratis.chronicle.keys.Key`,
`@io.cratis.chronicle.compliance.Pii`, `@io.cratis.chronicle.Subject`,
`@io.cratis.chronicle.schemas.JsonSchemaType`.

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

`AppendOptions` (`eventSequences/AppendOptions.kt:41-51`, `@JvmOverloads`)
carries `correlationId`, `concurrencyScope`, `eventSourceType`,
`eventStreamType`, `eventStreamId`, `subject`, `tags`, `occurred`, `causation`.

A unit of work spans several appends:

```kotlin
val unitOfWork = store.unitOfWorkManager.begin()
store.eventLog.transactional.append("<id>", <EventName>(<value>))
store.eventLog.transactional.appendMany("<id>", listOf(<OtherEvent>()))
unitOfWork.commit()
```

`transactional` appends return `Unit`, not an `AppendResult`; the results are on
the unit of work (`transactions/IUnitOfWork.kt:23-101`).

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

That shape is the shipped sample verbatim
(`Samples/Kotlin/SpringBoot/.../WelcomePackageReactor.kt:16-22`).

**A returned event is appended as a side effect.** `null`, `Unit`, and any value
whose type is not annotated `@EventType` are ignored; a single event, an
`EventForEventSourceId`, or a `List` mixing both is appended, defaulting to the
triggering event source — `observation/ReactorSideEffects.kt:26-39`, `:48-59`.
This is how a reactor appends without ever touching the event log.

`EventContext` is a data class with `sequenceNumber: Long`, `eventSourceId`,
`eventType`, `occurred`, `correlationId`, `causedBy`, `eventSourceType`,
`eventStreamType`, `eventStreamId`, `eventStore`, `namespace`, `causation`,
`tags`, `hash`, `observationState` — `events/EventContext.kt:31-47`.

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
`FromAll`, `FromEvery`, `Nested`, `NoAutoMap`, `NotRewindable`. The declarative
alternative is a class implementing `IProjectionFor<TReadModel>`.

## Discovery and registration

`ClientArtifacts` scans the classpath with ClassGraph
(`artifacts/ClientArtifacts.kt:44-48`), and `ClientArtifacts.default` is a
process-wide lazy singleton (`:156`). What it looks for (`:66-98`):

| Kind | Rule |
| --- | --- |
| event types | `@EventType` |
| event type migrations | implements `IEventTypeMigration` |
| read models | `@ReadModel` |
| declarative projections | implements `IProjectionFor` |
| model-bound projections | `@FromEvent` **and** the synthetic `FromEvent$Container` |
| reactors | `@Reactor` |
| reducers | `@Reducer` |
| constraints | implements `IConstraint` |
| seeders | implements `ICanSeedEvents` |
| webhooks | implements `IWebhookDefiner` |
| captures | implements `ICapture` |

> The `FromEvent$Container` entry is not incidental: Kotlin's `@Repeatable`
> replaces repeated annotations with a synthetic container, so a class carrying
> more than one `@FromEvent` is **not** annotated with `@FromEvent` at runtime.
> A scan that looks only for the annotation silently misses every multi-event
> projection.

Registration order is fixed and matters
(`artifacts/ArtifactRegistrations.kt:59-91`): event types and migrations →
unowned read models → constraints → model-bound constraints → projections →
webhooks → reactors → reducers → captures → seeders. Reactors and reducers are
started only on the first pass (`:78-82`).

**Registration re-runs on every reconnect.** `EventStore.kt:226-241` launches a
coroutine on `Dispatchers.IO` collecting the connection lifecycle and
re-registers each time. The connection id rotates on every disconnect because
the kernel keys observer subscriptions by it
(`connection/ConnectionLifecycle.kt:24-32`), so observers must re-register — and
they do.

`store.awaitRegistration()` (`IEventStore.kt:92`) waits for the first pass.
`store.registerAll()` (`:83`) runs it by hand when
`autoDiscoverAndRegister = false`.

## Java

Java is a first-class target here, not an afterthought: there is a compile-only
Java conformance suite under `Source/src/test/java` whose whole point is stated
in `conformance/JavaConformance.java:71-74` — *"It is never run — compiling it is
the assertion"*.

**Start Java code at `BlockingChronicleClient`**, not at the raw `ChronicleClient`
plus static bridges. Verbatim from the compile-checked fixture
(`Source/src/test/java/io/cratis/chronicle/java/JavaClientFlowUsage.java:29-34`):

```java
var client = BlockingChronicleClient.connect(ChronicleOptions.development());
var eventStore = client.getEventStore("<EventStoreName>");

eventStore.getEventLog().append("<event-source-id>", new <EventName>("<value>"));
```

`BlockingChronicleClient` is `AutoCloseable`, so `try (var client = ...)` works
(`java/BlockingChronicleClient.kt:35`, `connect` at `:74-76`). The blocking
surface continues through `BlockingEventStore`, `BlockingEventSequence`,
`BlockingReadModels`, `BlockingReactors`, `BlockingReducers`,
`BlockingUnitOfWork`, and `AppendOptionsBuilder`.

> The repository's own README shows the **older** low-level route —
> `new ChronicleClient(...)` plus `EventStoreJavaBridge` / `EventLogJavaBridge`
> (`README.md:225-244`). Both APIs are real, but the reference documentation and
> the compile-checked fixture both start at `BlockingChronicleClient`. Write new
> Java against that; reach for the `*JavaBridge` statics only for a corner the
> blocking client does not wrap.

In Spring Boot, Java injects the `Chronicle` bean instead — it already takes
`Class<T>` and returns plain values.

## Connection lifecycle

- **Keepalive is two-way.** The kernel pushes a keep-alive down the `Connect`
  stream and the client answers with a separate unary `connectionKeepAlive` RPC
  (`connection/ConnectionManager.kt:30`). A watchdog checks every
  `WATCHDOG_INTERVAL_MS = 1_000L` and treats a gap longer than
  `KEEP_ALIVE_TIMEOUT_MS = 5_000L` as a lost connection (`:126-128`, `:164`,
  `:171`). **Silence, not an error, is how the connection dies** — nothing throws.
- Reconnect is an infinite loop with jittered exponential backoff, base 1s,
  capped at 30s, re-resolving DNS/SRV on every attempt
  (`ConnectionManager.kt:75-101`, `:155-158`).
- The client identifies itself to the kernel as `"Kotlin"`
  (`ConnectionManager.kt:161`).
- `dispose()` cancels the connection manager, shuts the channel down with a
  5-second `awaitTermination`, then `shutdownNow`
  (`connection/ChronicleConnection.kt:116-125`).
- **Your process must stay alive** for reactors and reducers to keep receiving —
  observation is a live gRPC stream, and each observer runs on its own
  `CoroutineScope(Dispatchers.IO)` (`observation/ReactorsService.kt:57`,
  `observation/ReducersService.kt:59`).

## Testing

`io.cratis:chronicle-testing` runs in-process with no kernel and no Docker:
`EventScenario` (`Testing/.../EventScenario.kt:35`) and
`ReadModelScenario<TReadModel>` (`Testing/.../ReadModelScenario.kt:47`), which
folds a reducer through the same handler-shape rules as production
(`ReadModelScenario.kt:139-150`). Its scope is small — appends and reducer folds
only; there is no in-process reactor, projection, or constraint scenario
(`Testing/api/Testing.api` is 56 lines).

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Naming a reactor method after the event | The name is ignored; **the first parameter's type** is the subscription (`EventHandlerMethod.kt:61-74`) |
| Two `@EventType` classes sharing a simple name | The id defaults to the simple name, so they collide on the wire (`EventTypesService.kt:67`) |
| Shipping the default connection string to production | `skipTlsValidation` defaults to **true**; certificate validation is off (`ChronicleConnectionString.kt:28`) |
| Copying a version from the README or docs | Both are stale at `v4.0.0`; the real version comes from the release, not the source |
| Expecting `getEventStore` to suspend | It does not — the client already connected in its constructor (`ChronicleClient.kt:13`) |
| Expecting `awaitRegistration()` to mean "registered" | It completes in a `finally`, so it also returns after a failed pass (`ArtifactRegistrations.kt:47-57`) |
| Expecting a registration failure to throw | Failures are printed to `System.err`, not raised (`EventStore.kt:236`) |
| Expecting read model reactors to be discovered | `IReadModelReactor` is absent from the scan and from `IEventStore`; construct `ReadModelReactors(...)` yourself |
| Leaving the default classpath scan on in a large app | Use `withArtifactsFrom(...)` or `artifact-packages` to narrow it (`ChronicleOptions.kt:58`) |
| Expecting WebFlux support from the starter | The web auto-configuration is `SERVLET`-only (`ChronicleWebAutoConfiguration.kt:29`) |
| Exiting `main` after an append | Reactors and reducers stop with the process; the stream is live |

## Verify

- The dependency resolves from Maven Central and the version is the one you
  intended — not a number copied from a README.
- A connect against the target kernel succeeds, and the client/server version
  pair is one you confirmed rather than assumed.
- A production connection string sets `skipTlsValidation=false`.
- Every `@EventType` class has a unique simple name, or an explicit `id`.
- Every reactor and reducer handler's **first parameter** is an `@EventType`
  class.
- `awaitRegistration()` is followed by a check that registration actually
  succeeded, not treated as proof on its own.
- The build is clean and the specifications pass against the verified artifact
  version.
