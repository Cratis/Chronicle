---
name: cratis-chronicle-client-kotlin
description: Connect Kotlin and Java applications to Chronicle with the standalone client or Spring Boot starter, including events, reactors, reducers, projections and variants. Use when connecting standalone Kotlin coroutines or Java blocking calls to Chronicle through io.cratis:chronicle and classpath discovery. Do not use for Elixir OTP (use cratis-chronicle-client-elixir) or Node.js decorators (use cratis-chronicle-client-typescript); Arc commands belong to cratis-arc-command-kotlin, not this client skill.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/SKILL.md -->

# The Chronicle client for Kotlin and Java

`io.cratis:chronicle` is a **standalone client SDK**. Your application is an
ordinary JVM application that happens to talk to a Chronicle server over gRPC.
There is no framework to inherit from and no application architecture imposed:
you construct a client, ask it for an event store, and append or observe.

## Verified product sources

Source: `Cratis/Chronicle.Kotlin`, by source symbols and signatures.

Read [references/product-sources.md](references/product-sources.md) when you need the artifacts and their source modules.

> **Do not copy a version number out of the repository's README or docs.**
> The version comes from a Gradle property injected at release time —
> `Source/build.gradle.kts` reads
> `providers.gradleProperty("version")` and defaults to `0.0.0-SNAPSHOT` — so
> the checked-in source never carries the real number. Take it from Maven
> Central.

> **The client and the kernel version independently.** `CompatibilityPreflight` checks contracts
> at connect and throws `ChronicleServerIncompatible`; do not infer a supported
> pair just because both products are "latest".

Requires Chronicle kernel 19.30.0 for event source definitions and streams.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Naming a reactor method after the event | The name is ignored; **the first parameter's type** is the subscription (`EventHandlerMethod.kt`) |
| Two `@EventType` classes sharing a simple name | The id defaults to the simple name, so they collide on the wire (`EventTypesService.kt`) |
| Shipping the default connection string to production | `skipTlsValidation` defaults to **true**; certificate validation is off (`ChronicleConnectionString.kt`) |
| Copying a version from the README or docs | It can lag the release; take the version from Maven Central |
| Supplying `apiKey` in the connection string | Client construction rejects it with `IllegalArgumentException`; use `user:password@` |
| Expecting `getEventStore` to suspend | It does not — the client already connected in its constructor (`ChronicleClient.kt`) |
| Expecting `awaitRegistration()` to prove all artifacts registered | The initial gate completes in `finally` even after failure; connection rejection still throws `ChronicleConnectionFailed` |
| Treating automatic and explicit registration alike | The automatic pass logs failures; explicit `registerAll()` throws `ArtifactRegistrationFailed` for failed observers, and rejected event-type registration throws `ChronicleCommandRejected` |
| Expecting read model reactors to be discovered | `IReadModelReactor` is absent from the scan and from `IEventStore`; construct `ReadModelReactors(...)` yourself |
| Leaving the default classpath scan on in a large app | Use `withArtifactsFrom(...)` or `artifact-packages` to narrow it (`ChronicleOptions.kt`) |
| Expecting WebFlux support from the starter | The web auto-configuration is `SERVLET`-only (`ChronicleWebAutoConfiguration.kt`) |
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

Read [references/connection-string.md](references/connection-string.md) when writing a connection string or choosing credentials. A production string must set `?skipTlsValidation=false`; `apiKey` is rejected at client creation.

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

`io.cratis.chronicle.events.EventType` — `events/EventType.kt`:

```kotlin
annotation class EventType(
    val id: String = "",
    val generation: Int = 1,
    val tombstone: Boolean = false
)
```

**The id defaults to the class's *simple* name, not its fully qualified name** —
`events/EventTypesService.kt` resolves `ann.id.ifEmpty { cls.simpleName!! }`.
Two event classes with the same simple name in different packages therefore
collide on the wire. Give one an explicit `id`.

Read [references/event-type-annotations.md](references/event-type-annotations.md) when choosing an event class shape or its property annotations.

`@Pii` on a `ConceptAs` type is declared once and cascades to its uses.
For operational secrets (API keys, partner credentials), use
`@io.cratis.chronicle.confidentiality.Encrypted(scope = EncryptionScope.Subject)`;
`EncryptionScope` also offers `Namespace` and `Global`. Encryption at rest is
not right-to-erasure: use `@Pii` for personal data. A value cannot combine
`@Pii` and `@Encrypted`, and neither is supported on an `EventSourceId` concept.

Evolving a schema is `IEventTypeMigration<TTarget, TSource>` with `upcast` and
`downcast` — `events/migrations/IEventTypeMigration.kt`. Migrations
register in the same call as event types
(`artifacts/ArtifactRegistrations.kt`).

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
  (`IEventSequence.kt`). Typed alternatives taking `ConceptAs<String>` exist
  as extension functions in `io.cratis.chronicle.concepts`
  (`concepts/EventSourceIdConcepts.kt`).
- `AppendResult` carries `sequenceNumber`, `constraintViolations`, `errors`,
  `isSuccess`, `concurrencyViolation` — `eventSequences/AppendResult.kt`.
  It also carries `sequenceNumberValue: Long` purely so Java can read
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

Requires Chronicle.Kotlin 6.0.0 for kernel-resolved routing.

Omitted/empty routing resolves **in the kernel** to
`Default` / `All` / `Default` (source type / stream type / stream id).
The client no longer substitutes the event source id as stream id. Preserve a
legacy route by passing all three explicitly. Reads narrow only by supplied
filters; single-source `appendMany` applies the routing options to every event.
See the product's `Documentation/guides/migrate-append-routing.md`.

Read [references/append-concurrency.md](references/append-concurrency.md) when an append must be an idempotent create (`withExpectsNoMatchingEvent()`).

Event-source definitions and their named streams are optional, discovered
artifacts (kernel requirement above).
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
method name. `observation/EventHandlerMethod.kt` reads a function as a
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
event rejects all — `observation/ReactorSideEffects.kt`.
This is how a reactor appends without ever touching the event log.

Read [references/annotations-and-handlers.md](references/annotations-and-handlers.md#eventcontext) when you need the `EventContext` fields.

### The annotations

Read [references/annotations-and-handlers.md](references/annotations-and-handlers.md) when choosing an observer annotation (`@Reactor`, `@Reducer`, `@ReadModel`, `@Projection`, `@Constraint`, `@Seeder`), its arguments, or the reducer handler shapes.

### Read models and model-bound projections

Read [references/read-models-and-projections.md](references/read-models-and-projections.md) when querying read models through `store.readModels`, declaring a model-bound or `IProjectionFor` projection, or needing a passive model for read-your-writes.

### Variants — mutually exclusive read models for one entity's lifecycle

Variants let several read models share one identity and be mutually exclusive; each variant must declare an `@EntersOn`/`entersOn(...)` event or registration throws `VariantMustDeclareEntersOnEvent`.

Requires Chronicle.Kotlin 6.5.0 for end-to-end variant key support; replay affected projections once when moving from an earlier version.

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
`EventScenario` (`Testing/.../EventScenario.kt`) and
`ReadModelScenario<TReadModel>` (`Testing/.../ReadModelScenario.kt`), which
folds a reducer through the same handler-shape rules as production
(`ReadModelScenario.kt`). Its scope is small — appends and reducer folds
only; there is no in-process reactor, projection, or constraint scenario
(`Testing/api/Testing.api`).

Requires Chronicle.Kotlin 6.5.0 for `EventScenario` assertions and
`eventsOf` to count only events appended after the last `given`.

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
