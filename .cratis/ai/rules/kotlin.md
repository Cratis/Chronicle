---
applyTo: "**/*.kt,**/*.kts"
paths:
  - "**/*.kt"
  - "**/*.kts"
---
<!-- cratis-ai-managed: rules/kotlin.md -->

# Kotlin Conventions

Kotlin is a JVM-first, coroutine-first language. Use its own idioms — data
classes, `null`-aware types, coroutines, sealed hierarchies — instead of
carrying Java or C# patterns across unchanged. A Cratis Arc or Chronicle
application on Kotlin follows Spring Boot conventions for wiring and this
file's conventions for everything else; Arc/Chronicle-specific shapes
(`@Command`, `@ReadModel`, `@EventType`, and the rest) live in the matching
skill, not here.

## Building

- Use `./gradlew build` from the command line; it is the definition of "the
  build is clean" — zero warnings, zero errors.
- Use `./gradlew test` to run tests.
- Treat compiler warnings as errors. `allWarningsAsErrors` is the house
  default for Kotlin compilation; do not add a suppression to get to green.

## Formatting

- Four-space indentation, no tabs.
- One top-level public declaration per file, named after that declaration.
- Sort imports alphabetically; no wildcard imports.
- Trailing commas in multi-line parameter lists and call sites — they keep
  diffs to one line when a member is added or removed.
- Insert a blank line before the opening `{` of a multi-line `if`/`when`/`for`
  block; keep single-expression bodies on one line.

## Language — American English Only

All identifiers, KDoc, and string literals use **American English** spelling
(initialize, serialize, behavior, color, organization, center, modeling,
dialog, license, judgment, gray). See [general.md](./general.md).

## Naming

- PascalCase for class, interface, and object names.
- camelCase for functions, properties, and local variables.
- No `I` prefix on interfaces — Kotlin does not carry that convention; name
  the interface for what it does (`CommandPipeline`, not `ICommandPipeline`)
  unless a concrete/interface pair genuinely needs `Default<Name>` for the
  implementation.
- Never suffix a class with `Impl`, `Service`, or `Async` purely to
  disambiguate — name it for its role.

## Visibility

- Kotlin's implicit-public default is what most declarations should use;
  spelling `public` is a style choice, not a requirement — decide once per
  file or module and stay consistent within it.
- Mark a declaration `internal` when it must not cross a module boundary, and
  `private` for anything a class does not need to expose. Do not default
  everything to `public` "to be safe" — a wider surface than necessary is a
  bigger contract to keep stable.
- Compile-time discovery (KSP, reflection-based scanning) generally requires
  **public** top-level types and members. A declaration silently excluded from
  discovery because it is `internal` or `private` is a visibility bug, not a
  framework limitation — check the matching skill's discovery rules before
  assuming a narrower visibility is safe.

## Nullability

Kotlin's type system is the first line of defense against null-related bugs —
trust it the same way C# nullable reference types are trusted.

- Prefer a non-nullable type and a real default over a nullable one with a
  defensive check.
- Use `?.`, `?:`, and `requireNotNull()`/`checkNotNull()` instead of manual
  `if (x != null)` guards.
- A nullable outer collection (`List<T>?`) is supported; a nullable *element*
  inside a collection (`List<T?>`) is a narrower, often-unsupported shape in
  generated/serialized contracts — do not reach for it without checking
  whether the surface you are writing actually allows it.
- Data classes model state, not behavior — keep validation and invariants in
  the type that owns them (a concept, a validator), not scattered across every
  call site that happens to touch a nullable field.

## Classes and Data

- Prefer `data class` for immutable value holders (DTOs, events, command
  payloads, read models) — value equality, `copy()`, and `componentN()`
  destructuring come for free.
- Prefer a primary constructor with `val` properties over a body full of
  assignment statements.
- Reach for a `sealed class`/`sealed interface` hierarchy instead of an enum
  with a payload bolted on, whenever the variants carry different data.
- Favor composition over inheritance; an `interface` plus a `Default*`
  implementation is the house shape for an overridable collaborator, not a
  base class meant to be extended.

## Coroutines

- An asynchronous API is `suspend`, not a returned `Future`/`CompletableFuture`
  — Kotlin callers compose `suspend` functions directly, and a JVM-facing
  bridge (`CompletionStage`) is layered on top for Java callers, never the
  other way around.
- Per-call state travels in a `CoroutineContext` element; never a
  `ThreadLocal`. A coroutine can move between threads, and thread-local state
  silently goes stale when it does.
- Never launch on `GlobalScope`. Use the scope the host (Spring Boot's bounded
  application coroutine scope, a test's `runBlocking`/`runTest`) already
  provides.
- Cleanup that must run even after cancellation goes in
  `withContext(NonCancellable) { ... }`; everything else should stay
  cancellable rather than swallow the cancellation.
- Bridge a genuinely thread-bound third-party API with a
  `ThreadContextElement`; do not leak a raw `ThreadLocal` into coroutine-facing
  state as a shortcut.

## Java Interop

Kotlin/Java interop is a first-class concern whenever a library or an
application may be consumed from Java — which every Arc/Chronicle application
should assume by default:

- Add `@JvmStatic` to companion-object factories a Java caller needs to call
  without `Companion.`.
- Add `@JvmOverloads` to a function with default parameter values so Java
  gets the shorter overloads too.
- Give a function returning a value class or an inline class a `@JvmName`
  when Java needs to call it — the mangled synthetic name is not meant to be
  called directly.
- Prefer a `List<T>`/`Map<K, V>` parameter type over a Kotlin-only collection
  interface on any Java-facing surface.

## KDoc

- Every public type, function, and property meant for consumption outside its
  module gets a KDoc comment (`/** ... */`) — what it does and why, not a
  restatement of its name.
- Keep the summary to one or two sentences; use `@param`, `@return`, and
  `@throws` for the rest.

## Exceptions

- Use exceptions for genuinely exceptional, unrecoverable conditions — not for
  control flow a caller is expected to handle. A command or query rejection a
  user can act on is a validation result, not a thrown exception; see the
  Arc validation skills.
- Define a custom exception type when a caller needs to distinguish it from
  other failures; do not throw a bare `RuntimeException`/`IllegalStateException`
  for something callers are expected to catch specifically.

## Dependency Injection

- Constructor injection through the primary constructor is the default; avoid
  field/property injection.
- Give a collaborator an interface and a `Default*` implementation when an
  application might reasonably override it, bound with Spring's
  `@ConditionalOnMissingBean` so a consumer can replace it without forking.

## Logging

- Log through the host's structured logging facility (SLF4J via Spring Boot),
  not `println`.
- Log at the boundary where a decision is made or an error is handled, not at
  every intermediate call.

## Testing

- JUnit 5 is the house test runner for Kotlin and Java alike.
- A Kotlin test lives beside its Kotlin production code
  (`src/test/kotlin/.../<Thing>Test.kt`); a Java-facing surface also gets a
  Java test that compiles and runs against it, because Kotlin-only tests do
  not prove Java call-site compatibility.
