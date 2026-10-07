---
agent: agent
description: Write comprehensive BDD specs for an existing vertical slice command, query, projection, or reactor, starting from the slice's contract.
---
<!-- cratis-ai-managed: prompts/write-specs.prompt.md -->

# Write Specs

Write **comprehensive specs** for an existing slice. Invoke the **cratis-application-slice-specifications** skill (and **cratis-chronicle-event-specifications** / **cratis-chronicle-read-model-specifications** for constraints and projections); follow `.cratis/ai/rules/specs.md` and `.cratis/ai/rules/specs.csharp.md`.

## What to provide

The slice file (`.cs`) **and its contract**: the `.play` slice with its specifications when the slice is modeled (search the model root, default `.cratis/screenplay/`), otherwise the agreed slice outline.

If no contract can be found, decide once:

- **An accepted model under the model root covers the scope, or the repository is opted in** (the model root (default `.cratis/screenplay/`) holds a committed `.play` file (`git ls-tree -r --name-only HEAD -- <root>` lists it) or the project explicitly set `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install output, an installed skill, a `.play` file outside the root or an untracked or uncommitted draft is not opt-in (master: `cratis-screenplay-modeling-lifecycle`)): do not write code-derived specs in its place. Route to the `cratis-screenplay-*` skills (`cratis-screenplay-specifications`, `cratis-screenplay-scenario-coverage`) to add the slice's specifications to the model first. If those skills are not installed, say so and stop; do not author `.play` from memory.
- **No model coverage and no opt-in:** stay code-first. Write from the code, report every case as a proposal rather than as agreed behavior. A model is proposed only by the entry-point session, at most once per session, and not for trivial, bug-fix, infrastructure, client, framework or brownfield-maintenance work.

## Coverage (every slice type)

Lead with the in-process scenario family: `CommandScenario<T>` (state change), `EventScenario` (constraints), `ReadModelScenario<T>` (projections/reducers), `ReactorScenario<T>` (reactors). Reserve out-of-process Chronicle integration specs for host/transport boundaries.

0. **Every contract specification first**, one spec each, named after it, using the contract's example values (fresh values only where uniqueness requires). Then add the code-derived cases below that the contract lacks, and report them as proposals for the contract (the `.play` model when one covers the slice, otherwise the agreed outline); never leave them as silent coverage.
1. Happy path with each appended event asserted.
2. One spec per validator rule, asserting **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`. Build the command valid in every other respect so the spec violates only the rule it is named after.
3. One spec per constraint (`ShouldHaveConstraintViolationFor(name)`); authorization via `ShouldNotBeAuthorized()`.

Spec files are wrapped in `#if DEBUG`. Run the specs and fix failures before completing. The skill carries the detail; don't duplicate it here.
