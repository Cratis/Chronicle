---
name: cratis-arc-testing-kotlin
description: Specify Kotlin or Java Arc commands and queries through real in-process pipelines with arc-testing — CommandScenario, QueryScenario, bounded ObservableQueryScenario collection, read-model pins, tenant resolution, and Chronicle append assertions. Use when writing or diagnosing JVM Arc pipeline specifications without Spring Boot or a kernel. Do not use for standalone Chronicle client tests, C# scenarios, or defining the command or query itself.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-testing-kotlin/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Specify Arc behavior on the JVM

Use generated artifacts through the real pipelines, not hand-written handlers
that bypass KSP wiring. `arc-testing` needs neither Spring Boot nor Docker.
This is test support, not an alternative production host.

## Verified product sources

Source: `Arc.Kotlin`: `Testing/src/main/kotlin/io/cratis/arc/testing`
(`CommandScenario`, `QueryScenario`, `ObservableQueryScenario`, result assertions),
`Integrations/Chronicle/ChronicleCommandScenario.kt`, and
`Documentation/guides/testing.mdx`. Add
`testImplementation("io.cratis:arc-testing:<version>")`; align it with the runtime,
plugin and KSP version.

## Choose the pipeline

| Behavior | Scenario |
| --- | --- |
| Execute or validate a command | `CommandScenario(module, CommandType::class.java)` |
| One-shot query | `QueryScenario<T>` selected by `FullyQualifiedQueryName` |
| Streaming query | `ObservableQueryScenario<T>` with bounded `collect(maximumEmissions, timeoutMillis)` |

Application specs select a generated `ArcArtifactModule`. Manual
`CommandHandler`/`QueryPerformer` constructors are for framework tests.
Arrange services, validators, authorization policies, filters, principal,
tenant and correlation id on the scenario before executing it.

Illustrative excerpt: `module` is the generated module; `CreateTask` and its
`TaskRepository`/`TaskCreated` types are supplied by the application.

```kotlin
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import io.cratis.arc.testing.CommandScenario

val result = CommandScenario(module, CreateTask::class.java)
    .addService(TaskRepository::class.java, repository)
    .execute(CreateTask("Try Arc"))

result.shouldSucceed().shouldHaveResponse(TaskCreated::class.java)
```

Command and one-shot query scenarios perform JSON round trips by default,
including the module's registered derived types. Keep them enabled to exercise
wire behavior. Disable them only for a deliberate pure/in-process assertion;
serialization failures propagate to the test, not a successful result.
Observable scenarios do not perform opening-argument or result-data round trips.

## Pin current state, including absence

- `withReadModel(type, value)` pins a type regardless of command key.
- `withReadModelForKey(type, key, value)` proves key selection too; other keys
  resolve to absence. Scalar keys match concept-wrapped keys after unwrapping.
- A `null` pin is deliberate absence. Required parameters fail with
  `dependencyUnavailable`; Kotlin nullable parameters receive `null`, and
  Java `Optional` parameters receive `Optional.empty()`.
- Pins claim ownership and win over fallback resolvers. Do not mix a global
  pin and keyed pins for the same type. Use `addReadModelResolver` for computed state.

## Assert the rejecting stage

Use positive and negative assertions rather than only `shouldFail()`:
`shouldBeAuthorized`/`shouldBeUnauthorized`, `shouldBeValid`/`shouldBeInvalid`,
`shouldHaveErrors`/`shouldHaveNoErrors`.
`shouldBeAuthorized().shouldBeInvalid()` states that authorization passed and
a validation rule rejected the input.

`CommandScenarioResult.shouldBeInvalid()` fails when all feedback is
`dependencyUnavailable`: that normally means the test forgot a dependency,
not that a rule ran. Pin or register it before asserting the rule. When missing
dependencies are the intended behavior, assert
`shouldHaveValidation(reason = ValidationResultReasons.DEPENDENCY_UNAVAILABLE)`.

`withTenantResolution` takes a resolver and explicit request context on all
three scenarios. The resolved tenant also becomes the namespace unless one is
supplied. Test unresolved and unauthorized tenant selection as well as success;
use `cratis-arc-identity-and-tenancy-kotlin` for the request contract.

## Bound observable specifications

One timeout budget covers opening and collection. Timeout/cancellation
propagates and cancels cooperative upstream work; it is not a successful result.
`maximumEmissions` is a cap, not a required count: a finite stream can finish
empty or early. Chain `shouldHaveEmissionCount(expected)` when count matters.
`addEmissionGuard` supplies per-emission checks; assert opening rejection with
`shouldFail()` and terminal guard denial with `shouldTerminateUnauthorized()`.

## Chronicle integration without a kernel

Add the Chronicle starter (or `io.cratis:cratis`) alongside `arc-testing`.
Its `CommandScenarioExtender` is found through `ServiceLoader` and supplies
an in-memory event log automatically. `givenChronicle().events(id, event)`
establishes prior history; `chronicle().shouldHaveAppendedEvent(id, Event::class.java)`
checks the new append. Given events are excluded from `appendedEvents`.
Arrange `constraintViolation(...)` or `concurrencyViolation(...)` to reject an
append deterministically and assert `shouldHaveConstraintViolation(...)` or
`shouldHaveConcurrencyViolation(...)`. Rejected appends are not appended events.
This does not verify a real kernel constraint or storage integration.

## Java and verification

Use the closeable `BlockingCommandScenario` over a configured scenario for
ordinary Java tests; configure pins, tenant resolution and Chronicle before
constructing the bridge. `ChronicleCommandScenarios` exposes Kotlin extensions
as Java statics. For asynchronous tests use `AsyncCommandScenario`,
`AsyncQueryScenario`, `AsyncObservableQueryScenario` and a `JavaAsyncScope`.
`usingExecutor` borrows an executor; `owningExecutorService` owns shutdown.
Scenarios borrow the scope; close the owner after the test.
`collectAsync` returns a `CompletionStage` with bounded collection. Timeout
cancels the stage; callback-based `collect` does not deliver timeout callbacks.

- Follow the `for_`/`when_` naming conventions in `kotlin.md`/`java.md`.
- Run `./gradlew build` with warnings treated as errors and all affected specs.
- Verify authorization, rejection, absence and failure paths that apply, not
  only a successful response.
- Run real-store integration checks separately when the behavior depends on
  kernel constraints, persistence, transactions or change streams.

For definition rather than tests: `cratis-arc-command-kotlin`,
`cratis-arc-query-kotlin`, `cratis-arc-validation-kotlin`.
