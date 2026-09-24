---
name: cratis-arc-command-kotlin
description: Define a Cratis Arc command in Kotlin or Java — the @Command class, its handle() method, the optional provide() step, Chronicle event responses, and command authorization. Use when adding a command to a Kotlin or Java Arc application, choosing what handle() should return, or wiring Chronicle event append from a command. Do not use for the .NET Arc command shape, for validation-only changes, or for query/read-model definition.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command-kotlin/SKILL.md -->

# Define an Arc command in Kotlin or Java

Arc.Kotlin brings the same command shape as Arc .NET to the JVM: a class carries
the user's intent and owns its own handler. KSP (the Kotlin Symbol Processor)
discovers it at compile time and generates a reflection-free `CommandHandler` —
there is no controller or handler class to write by hand.

## Verified product sources

Verified against `Arc.Kotlin` at tag **`v7.3.0`**, re-verified at commit
`d0aa6a4` (14 commits past the tag), against the documented consumer setup:
JDK 17, Gradle 8.14.4, Kotlin 2.4.20, KSP 2.3.12, Spring Boot 4.1.x. The
re-verification picked up a real behavior correction to authorization
override — see "Protect the command" below — confirmed against
`ArcSymbolProcessorAuthorizationPrecedenceCompilationTest` and
`buildAuthorization` in `ArcSymbolProcessor.kt`, not documentation prose alone:
two of Arc.Kotlin's own reference pages (`reference/annotations.md`,
`troubleshooting.md`) are currently stale on this point relative to
`guides/security.md`, `reference/parity.md`, and the compiler source. The local workspace version is `0.0.0-SNAPSHOT`; use a released version
from Maven Central for a real application.

| Artifact | Purpose |
| --- | --- |
| `io.cratis:arc` | `io.cratis.arc.artifacts.Command`, `CommandKey`, `FromServices` |
| `io.cratis:arc-ksp` | the KSP processor that generates the command handler |
| `io.cratis:arc-spring-boot-starter` | Spring Boot auto-configuration, the command pipeline host |
| `io.cratis:arc-chronicle-spring-boot-starter` | optional — Chronicle event append from a returned event |
| `io.cratis.arc` Gradle plugin | applies Kotlin/JVM + KSP, adds the dependencies, configures the module name and proxy output |

Arc.Kotlin has no Chronicle dependency by default. Add the optional Chronicle
starter only when a command should append a returned event or read a Chronicle
read model; without it, `handle()` returns an ordinary response value.

Java applications still need Kotlin and KSP on the build — Arc generates Kotlin
adapters for Java declarations even in an all-Java application. A Java project
uses the identical Gradle plugin and `cratisArc {}` block below; only the
application source files differ (Java records instead of Kotlin data classes).

## Set up the project

```kotlin
// settings.gradle.kts
pluginManagement {
    repositories {
        mavenCentral()
        gradlePluginPortal()
    }
}
```

```kotlin
// build.gradle.kts
plugins {
    id("io.cratis.arc") version "<version>"
    kotlin("plugin.spring") version "2.4.20"
    id("org.springframework.boot") version "4.1.1"
    id("io.spring.dependency-management") version "1.1.7"
}

repositories {
    mavenCentral()
}

cratisArc {
    moduleName.set("<ModuleName>")
    dependencyVersion.set("<version>")
    endpoints {
        segmentsToSkip.set(<packageSegmentsUnderModuleRoot>)
    }
}

dependencies {
    implementation("io.cratis:arc-spring-boot-starter:<version>")
    implementation("org.springframework.boot:spring-boot-starter-webmvc")
}
```

The plugin applies Kotlin/JVM and KSP, targets JDK 17, and treats warnings as
errors. `moduleName` names the generated artifact module and manifest; every
module in an application needs a distinct one. `endpoints.segmentsToSkip` must
match `cratis.arc.endpoints.segments-to-skip-for-route` in
`application.properties` — the Spring Boot starter fails startup with a named
mismatch when proxies were generated and the two settings disagree.

## Declare the command

```kotlin
package example.tasks

import io.cratis.arc.artifacts.Command
import io.cratis.arc.artifacts.CommandKey
import io.cratis.chronicle.events.EventType

@EventType
data class TaskCreated(val title: String)

@Command
data class CreateTask(@CommandKey val id: String, val title: String) {
    fun handle(): TaskCreated = TaskCreated(title)
}
```

```java
package example.tasks;

import io.cratis.arc.artifacts.Command;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;

@Command
public record CreateTask(String title) {
    public CompletionStage<TaskCreated> handle(TaskRepository repository) {
        var task = repository.create(title);
        return CompletableFuture.completedFuture(new TaskCreated(task.id(), task.title()));
    }
}
```

Rules the framework actually enforces:

- `@Command` (`io.cratis.arc.artifacts.Command`) targets a class — a Kotlin
  class or data class, or a Java record. The type must also declare a public
  instance `handle` method; KSP reports `ARCKSP0100` when a type looks
  command-like but lacks `@Command`.
- Only top-level **public** classes qualify. Kotlin's implicit-public
  visibility counts — spelling `public` is style, not a requirement — but
  private, internal, and Java package-private classes fail closed
  (`ARCKSP0101`/`ARCKSP0102`).
- `handle` resolves unannotated parameters from Spring by type, exactly like a
  constructor-injected `@Service`. There is no separate DI container to wire.
- `handle` may return `Unit`/`void`, a plain response value, a suspend result
  (Kotlin), or `CompletionStage<T>` (Java). The generated adapter runs inside
  Arc's bounded application coroutine scope — never `GlobalScope`, never
  thread-local request state.
- Name the command as the action — `CreateTask`, not `CreateTaskCommand`.
- KSP keeps constructor properties in declaration order, then appends public
  declared body properties in name order. A body `@CommandKey`, `@Valid`, and
  field/getter validation constraints are retained the same way constructor
  properties are; a constructor key plus a body key fails with `ARCKSP0106`.
- Mark server-only state `@field:JsonIgnore` or `@get:JsonIgnore`, or keep it
  nonpublic. A computed or read-only property reached through command input
  fails with `ARCKSP0300` — use a backed property, ignore the member, or
  return a separate output model instead.

## Fetch what the handler needs with `provide()`

`provide` is an optional public instance method that runs **after**
authorization and validation but **before** `handle`. `POST
<command-route>/validate` never invokes it. Its parameters resolve from Spring,
exactly like `handle`'s.

```kotlin
@Command
data class CompleteTask(val taskId: TaskId) {
    suspend fun provide(tasks: Tasks): Task = tasks.get(taskId)

    fun handle(task: Task, audit: AuditLog): TaskCompleted {
        audit.record(task.id)
        return TaskCompleted(task.id)
    }
}
```

- Return one value, a `Pair`, a `Triple`, `CommandProvidedValues`, or an
  `ArcOneOf` alternative. Provided values are matched to `handle`'s parameters
  in declaration order; each match is consumed once, and an unmatched
  parameter falls back to Spring.
- A returned `CommandResult`, `ValidationResult`, a non-empty validation-result
  collection, or `AuthorizationResult` is a control signal — it short-circuits
  `handle` instead of feeding it. This is how a `provide` rejects a request the
  user can act on; do not throw for that.
- Java retains `CommandProvidedValues.of(...)` / `CommandResponseValues.of(...)`
  factories; Kotlin has `commandProvidedValuesOf(...)` /
  `commandResponseValuesOf(...)` helpers for explicit ordered aggregates.

## Return a Chronicle event

Requires the optional `io.cratis:arc-chronicle-spring-boot-starter`. A plain
event response needs a stable command key backed by `String`, `UUID`, a
number, or a concept wrapping one of those — mark it `@CommandKey`.

```kotlin
@Command
data class CreateTask(@CommandKey val id: String, val title: String) {
    fun handle(): TaskCreated = TaskCreated(title)
}
```

Without a usable `@CommandKey`, Arc returns an error validation result
(`reason: "rule"`, `reasonDetail: "commandKey"`) rather than guessing an
event-source identifier.

| Return | What Arc does |
| --- | --- |
| A single `@EventType` value | Appended to the command key |
| A non-empty collection/array of `@EventType` values | All appended to the command key, atomically |
| `EventForEventSourceId(id, event)` | Appended to `id` instead — the cross-stream write |
| A collection mixing plain events and `EventForEventSourceId` | Plain events use the command key, wrapped events keep their own id; appended atomically through the cross-stream path |
| `EventsWithConcurrencyScopes` | Routed events only, with explicit per-source-id concurrency scopes |
| Anything else, or a value that mixes an event with a non-event | Becomes (or corrupts) the response — a mixed response containing a non-`@EventType` value fails before anything is appended |

```kotlin
import io.cratis.arc.chronicle.eventsWithConcurrencyScopes

fun handle(): EventsWithConcurrencyScopes = eventsWithConcurrencyScopes {
    event("account-42", FundsWithdrawn(100))
    event("ledger-2025", LedgerEntryAdded("account-42", 100))
    concurrencyScope("account-42") {
        withEventSourceId()
    }
}
```

Java builds the same shape with `EventsWithConcurrencyScopes.builder()` and
fluent `event(...)`.

Declare stable Chronicle routing defaults on the command when its events share
them, rather than repeating metadata on every `EventForEventSourceId`:

```kotlin
@Command
@CommandEventSourceType("Task")
@CommandEventStreamType("Tasks")
@CommandEventStreamId("active")
@CommandEventSubject("task-owner")
data class CreateTask(@CommandKey val id: String, val title: String) {
    fun handle(): TaskCreated = TaskCreated(title)
}
```

Each annotation is optional and must be nonblank with no control characters
when present (`ARCKSP0110` otherwise). Omit one to keep Chronicle's own
fallback: source/stream type default to `Default`, stream id/subject default
to the event-source identifier. An explicit `EventForEventSourceId` value keeps
its own metadata; only its missing fields inherit the command's declaration.
Implement `CommandEventStreamIdProvider` or `CommandEventSubjectProvider`
instead when a value depends on the command instance — never combine a
provider with the matching static annotation (`ARCKSP0110`).

A returned event, a mixed collection, or `EventsWithConcurrencyScopes` commits
through one staged transaction per outermost command-execution root: any child
or root failure discards every staged event, and a successful root appends
once with `appendMany`. Constraint violations surface as a validation result
with `reason: "constraintViolation"`; concurrency failures surface as
`concurrencyViolation` with expected/actual sequence numbers.

## Inject a Chronicle read model into the handler

When `ChronicleOptions` lists a read-model artifact, `handle` (or `provide`)
can request the **current** instance for the command's own resolved key as an
unannotated parameter — resolution uses the generated command key and the
captured tenant store:

```kotlin
@Command
data class Withdraw(@CommandKey val accountId: String, val amount: Int) {
    fun handle(balance: AccountBalance): FundsWithdrawn {
        require(balance.available >= amount)
        return FundsWithdrawn(amount)
    }
}
```

A value from `provide` still takes precedence over this injection. A missing
model leaves the dependency unresolved rather than silently falling through to
another store; an invalid key or unavailable store fails the command.

## Protect the command

```kotlin
@Command
@Authorize(roles = ["admin"])
data class DeactivateAccount(@CommandKey val accountId: String) {
    fun handle(): AccountDeactivated = AccountDeactivated()
}
```

Use `@AllowAnonymous`, `@Authorize(policy = "...")`, `@Authorize(roles = [...])`,
or repeatable `@Roles`. **The operation replaces the class wholesale, in
either direction — this matches Arc .NET's `AuthorizationEvaluator.IsAuthorized(MethodInfo)`,
which resolves the method's own metadata first and falls back to the
declaring type only when the method declares none.** When `handle` declares
any authorization metadata of its own, only *its* policy/roles/schemes/anonymity
apply and the class declaration is discarded entirely — so an operation can
both narrow (a `@Roles("auditor")` operation on a class requiring `admin`
admits auditors and rejects admins) and *widen* access (an `@AllowAnonymous`
operation on an `@Authorize`-protected class is reachable without
authentication — the shape a login-screen query needs on an otherwise
protected read model):

```kotlin
@ReadModel
@Authorize
data class AuthenticationQueryItem(val message: String) {
    companion object {
        // Overrides the class: anyone may subscribe to this one.
        @JvmStatic
        @AllowAnonymous
        fun anonymous(@FromServices source: AuthenticationQuerySource): Flow<AuthenticationQueryItem> =
            source.observeAnonymous()
    }
}
```

Combining `@AllowAnonymous` with `@Authorize`/`@Roles` is only ever a
contradiction — and only ever fails with `ARCKSP0108` — **on the exact same
declaration** (both on the class, or both on the operation). Splitting them
across a class and its operation is not a conflict; it is the override
mechanism above.

## Generate the TypeScript proxy

```kotlin
cratisArc {
    moduleName.set("<ModuleName>")
    dependencyVersion.set("<version>")
    proxies {
        outputDirectory.set(layout.buildDirectory.dir("generated/arc-proxies"))
        segmentsToSkip.set(<packageSegmentsUnderModuleRoot>)
    }
}
```

An unset `proxies.outputDirectory` skips generation entirely. `proxies.segmentsToSkip`
controls the generated **file path**; the separate `endpoints.segmentsToSkip`
(and its runtime twin `cratis.arc.endpoints.segments-to-skip-for-route`)
controls the **HTTP route**. Mixing the two up produces the wrong file layout
or the wrong route, not a startup error — they are easy to confuse because
they read almost identically. The generated proxy's runtime contract
(`route`, `execute()`, `validate()`, `CommandResult`) is the same
`@cratis/arc` client shape as Arc .NET; see the `cratis-arc-command` skill for
that contract.

## Route near misses

- Adding or changing a rejection rule on an existing command:
  `cratis-arc-validation-kotlin`.
- Defining or changing a `@ReadModel` query: `cratis-arc-query-kotlin`.
- Talking to Chronicle outside a command (a plain worker, a reactor's own
  event types, direct append): `cratis-chronicle-client-kotlin`.
- The C# Arc command shape and its causation-chain rules: `cratis-arc-command`.

## Verify

- The command is a public top-level class or Java record, carries `@Command`,
  and declares a public instance `handle`.
- Every returned event type carries `@EventType`, and a plain event response
  has a usable `@CommandKey`.
- Cross-stream events are wrapped in `EventForEventSourceId`; a mixed
  collection contains only events and wrappers, nothing else.
- Rejections from `provide` or a validator are `ValidationResult`s, never
  thrown exceptions the user is meant to act on.
- `@AllowAnonymous` is not combined with `@Authorize`/`@Roles` on the exact
  same declaration (class+class or operation+operation) — splitting them
  across a class and its operation is the intended override, not an error.
- `endpoints.segmentsToSkip` (build) and `segments-to-skip-for-route` (runtime
  properties) agree; the application starts without the mismatch error.
- `./gradlew build` is clean with zero warnings and zero errors (Kotlin
  `allWarningsAsErrors`, Java `-Xlint:all -Werror`).
- The generated proxy exists at the configured output directory when one was
  configured, and the frontend compiles against it.
