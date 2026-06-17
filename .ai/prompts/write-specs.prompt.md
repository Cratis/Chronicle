---
agent: agent
description: Write comprehensive BDD specs for an existing vertical slice command, query, projection, or reactor.
---

# Write Specs

Write **comprehensive specs** for an existing slice. Invoke the **write-specs** skill (and `write-specs-events` / `write-specs-readmodels` for constraints and projections); follow `.ai/rules/specs.md` and `.ai/rules/specs.csharp.md`.

## What to provide

The slice file (`.cs`) to cover.

## Coverage (every slice type)

Lead with the in-process scenario family — `CommandScenario<T>` (state change), `EventScenario` (constraints), `ReadModelScenario<T>` (projections/reducers), `ReactorScenario<T>` (reactors). Reserve out-of-process Chronicle integration specs for host/transport boundaries.

- Happy path with each appended event asserted.
- One spec per validator rule, asserting **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`.
- One spec per constraint (`ShouldHaveConstraintViolationFor(name)`); authorization via `ShouldNotBeAuthorized()`.

Spec files are wrapped in `#if DEBUG`. Run the specs and fix failures before completing. The skill carries the detail; don't duplicate it here.
