---
applyTo: "**/*.java"
paths:
  - "**/*.java"
---
<!-- cratis-ai-managed: rules/java.md -->

# Java Conventions

Java is a supported, first-class Cratis application language alongside Kotlin
— an Arc.Kotlin-based application can be entirely Java source, with Kotlin and
KSP present only in the build (Arc generates Kotlin adapters for Java
declarations under the hood). Write idiomatic modern Java; do not import
Kotlin idioms into Java source, and do not treat Java as a second-class
citizen of a Kotlin codebase. Arc/Chronicle-specific shapes (`@Command`,
`@ReadModel`, `@EventType`, and the rest) live in the matching skill, not
here.

## Building

- Use `./gradlew build` (or the repository's Maven equivalent) from the
  command line; it is the definition of "the build is clean" — zero warnings,
  zero errors.
- Treat compiler warnings as errors (`-Xlint:all -Werror`); do not suppress a
  warning to get to green.

## Formatting

- Four-space indentation, no tabs.
- One public top-level type per file, named after that type.
- Sort imports alphabetically; no wildcard imports.
- Braces on the same line as the declaration (`if (...) {`), closing brace on
  its own line.

## Language — American English Only

All identifiers, Javadoc, and string literals use **American English**
spelling (initialize, serialize, behavior, color, organization, center,
modeling, dialog, license, judgment, gray). See [general.md](./general.md).

## Naming

- PascalCase for class and interface names.
- camelCase for methods, fields, and local variables.
- No `I` prefix on interfaces — name the interface for what it does.
- Never suffix a class with `Impl`, `Manager`, or `Helper` purely to
  disambiguate — name it for its role; reserve `Default<Name>` for the
  concrete counterpart of an interface meant to be overridden.

## Records and Immutable Data

- Prefer a `record` for an immutable value holder (DTOs, command payloads,
  query results, read models) over a hand-written class with a constructor,
  getters, `equals`, `hashCode`, and `toString` — the record gives all of that
  for free and keeps the file short.
- Give a record component a validating compact constructor when the type must
  never hold an invalid value, rather than validating at every call site that
  constructs one.
- Favor `sealed` interfaces with a closed set of `permits` implementations
  over a discriminator field, when a value can genuinely only be one of a
  known set of shapes — pattern matching over the sealed hierarchy replaces
  the `instanceof` chain.

## Nullability

- Prefer `Optional<T>` for a method's **return type** when absence is a normal
  outcome the caller must handle; never use `Optional` as a field type, a
  constructor parameter, or a method parameter — that is not what it is for.
- Prefer a non-null default and an explicit `Objects.requireNonNull(...)` at a
  constructor boundary over accepting `null` and checking for it throughout a
  class.
- On a surface consumed from Kotlin (an Arc/Chronicle read model, event, or
  command reached from Kotlin code), be deliberate about which fields are
  genuinely optional — a Java field with no null-safety annotation reads as
  non-null to Kotlin-facing generated code by default.

## Classes

- Favor composition over inheritance; an `interface` plus one or more
  implementing classes is the house shape for an overridable collaborator, not
  a base class meant to be extended.
- Make a class `final` unless it is deliberately designed for extension.
- Prefer constructor injection over field injection for a Spring-managed
  bean — a `final` field set in the constructor over an `@Autowired` field.

## Asynchronous Code

- `CompletionStage<T>` (or its `CompletableFuture` implementation) is the
  house shape for an asynchronous Java API, mirroring how Kotlin exposes
  `suspend` — Arc's generated adapters await a returned `CompletionStage`
  without reflection.
- Never block on a `CompletionStage` inside a method that itself returns
  one — compose with `.thenApply`/`.thenCompose` or `.thenAccept` instead of
  calling `.get()`/`.join()` inside the async chain.
- Give a Java caller its own explicit scope (an `ExecutorService`, or the
  scope type the host library provides) rather than reaching for a shared
  global one.

## Javadoc

- Every public type and method meant for consumption outside its package gets
  a Javadoc comment (`/** ... */`) — what it does and why, not a restatement
  of its name.
- Use `@param`, `@return`, and `@throws` for parameters, return values, and
  checked exceptions.

## Exceptions

- Use exceptions for genuinely exceptional, unrecoverable conditions — not for
  control flow a caller is expected to handle. A command or query rejection a
  user can act on is a validation result, not a thrown exception; see the Arc
  validation skills.
- Prefer an unchecked exception with a clear name over a checked exception
  that forces every caller to catch or declare it; reserve checked exceptions
  for conditions a caller genuinely has a recovery path for.

## Dependency Injection

- Constructor injection is the default for a Spring-managed bean; avoid field
  injection (`@Autowired` on a field) even though Spring supports it.

## Logging

- Log through the host's structured logging facility (SLF4J via Spring Boot),
  not `System.out`/`System.err`.
- Log at the boundary where a decision is made or an error is handled, not at
  every intermediate call.

## Testing

- JUnit 5 is the house test runner.
- A test for a Java-facing surface belongs beside the Java production code
  (`src/test/java/.../<Thing>Test.java`) and must actually compile and run
  against that surface — a Kotlin-only test does not prove Java call-site
  compatibility for a mixed-language library.
