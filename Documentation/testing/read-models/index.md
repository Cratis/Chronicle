---
uid: Chronicle.Testing.ReadModels
description: Test read model projections and reducers in process with seeded events, and identify which behavior still needs kernel-backed integration tests.
---
# Testing Read Models

Read-model scenarios turn a known event history into an assertable read model without starting Chronicle or a database. Use them for fast feedback on projection mappings and reducer logic, then use integration tests for the infrastructure behavior they substitute.

## Start with a small event history

For .NET, install the scenario package and the specification framework in your test project:

```bash
dotnet add package Cratis.Chronicle.Testing
dotnet add package Cratis.Specifications.XUnit
dotnet test
```

The central .NET API is `ReadModelScenario<TReadModel>`. Create a scenario for the read model under test, seed events with `Given.ForEventSource(id).Events(...)`, and assert on `Instance`. Reading the result triggers processing of the collected events. The scenario discovers a reducer, fluent projection, or model-bound projection; `WithProjection(...)` can select an inline projection instead, including when a reducer was discovered.

For example, a customer-summary test can seed a registration event followed by a name-change event for the same customer, then assert that the summary contains the new name. Keep the history focused on the behavior being specified; unrelated events make failures harder to explain.

## Choose the right result

`Instance` is convenient when the history materializes one model. If more than one instance materializes, it throws rather than selecting one arbitrarily. Use `Instances` or `InstanceForEventSourceId(...)` to select the intended result.

When event context matters, seed source and stream metadata explicitly or use `ProcessAppendedEvents(...)` to preserve original appended-event context. The [scenario guide](scenario.mdx) covers these APIs and includes client-specific examples; their names and capabilities differ between clients.

## Know the test boundary

A passing in-process test does not prove database persistence, network delivery, or the full kernel pipeline. In .NET, inspect `Substitutions` to see which substituted layers the tested model actually reaches. `WithStrictFidelity()` makes a dependency on such a layer fail the scenario instead of merely describing it. Use that information to choose the kernel-backed tests needed alongside the fast specification.

## Topics

- [ReadModelScenario and client examples](scenario.mdx)
- [Projections](../../projections/index.md)
- [Reducers](../../reducers/index.mdx)
- [Read models and PII](../../compliance/read-models.mdx)
