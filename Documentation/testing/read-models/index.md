---
uid: Chronicle.Testing.ReadModels
title: Test Chronicle read models from event history
description: Test Chronicle projection mappings and reducer logic with a known event history, then choose server-backed tests for storage and delivery behavior.
---

<a id="testing-read-models"></a>

Read-model scenarios check what a Chronicle projection or reducer produces from a known event history without starting a server or database. Use them to verify the resulting state; use server-backed integration tests to check how that state is stored and delivered.

## Start with a small event history

For .NET, install the scenario package and the specification framework in your test project:

```bash
dotnet add package Cratis.Chronicle.Testing
dotnet add package Cratis.Specifications.XUnit
dotnet test
```

Create a .NET `ReadModelScenario<TReadModel>` for the model under test, supply events with `Given.ForEventSource(id).Events(...)`, and assert on `Instance`. The events wait until you read a result; that read triggers the fold, which applies the history to produce the model's state. The scenario discovers a reducer, fluent projection, or model-bound projection. Use `WithProjection(...)` to select an inline projection instead, including when a reducer was discovered.

For example, a customer-summary test can seed a registration event followed by a name-change event for the same customer, then assert that the summary contains the new name. Keep the history focused on the behavior being specified; unrelated events make failures harder to explain.

## Choose the right result

In .NET, use `Instance` when the history produces one model. If it produces more than one keyed instance, `Instance` throws rather than choosing one. Select the intended result with `Instances` or `InstanceForEventSourceId(...)`. Reducers fed only bare events retain a single fold across sources unless you seed per-key read models; use `ProcessAppendedEvents(...)` when you need independent source folds from appended history.

When event context matters, seed source and stream metadata explicitly or use `ProcessAppendedEvents(...)` to preserve original appended-event context. The [scenario guide](scenario.mdx) covers these APIs and includes client-specific examples; their names and capabilities differ between clients.

## Know the test boundary

A correct projected value does not prove that a database stores the key correctly or that an observer recovers after a failure. The .NET harness uses the projection engine with an in-memory sink, or the client reducer invoker, rather than the full Chronicle kernel pipeline.

Inspect `Substitutions` to find model-specific dependencies on behavior the harness stands in for, such as join-key resolution or compliance protection. `WithStrictFidelity()` makes those dependencies fail the scenario when you read a result. An empty list does not mean that storage or observer delivery was tested. The [scenario guide's boundary section](scenario.mdx#what-the-harness-stands-in-for) explains which assertions need a kernel-backed companion test.

## Topics

- [ReadModelScenario and client examples](scenario.mdx)
- [Projections](../../projections/index.md)
- [Reducers](../../reducers/index.mdx)
- [Read models and PII](../../compliance/read-models.mdx)
