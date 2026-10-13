---
agent: agent
description: Write comprehensive BDD specs for an existing vertical slice command, query, projection, or reactor, starting from the slice's contract.
---
<!-- cratis-ai-managed: prompts/write-specs.prompt.md -->

# Write Specs

Write **comprehensive specs** for an existing slice. Invoke the **cratis-application-slice-specifications** skill (and **cratis-chronicle-event-specifications** / **cratis-chronicle-read-model-specifications** for constraints and projections); follow `.cratis/ai/rules/specs.md` and `.cratis/ai/rules/specs.csharp.md`.

## What to provide

The slice file (`.cs`) **and its contract**: the `.play` slice with its specifications when the slice is modeled (search the model root, the folder holding the project's `.play` files), otherwise the agreed slice outline.

If no contract can be found, decide once:

- **An accepted model under the model root covers the scope, or the repository is opted in** (the model root (the folder holding the project's `.play` files) holds a committed `.play` file (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured) or the project explicitly set `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install output, an installed skill, a `.play` file outside the root or an untracked or uncommitted draft is not opt-in (master: `cratis-screenplay-modeling-lifecycle`)): do not write code-derived specs in its place. Route to the `cratis-screenplay-*` skills (`cratis-screenplay-specifications`, `cratis-screenplay-scenario-coverage`) to add the slice's specifications to the model first. If those skills are not installed, say so and stop; do not author `.play` from memory.
- **No model coverage and no opt-in:** stay code-first. Write from the code, report every case as a proposal rather than as agreed behavior. A model is proposed only by the entry-point session, at most once per session, and not for trivial, bug-fix, infrastructure, client, framework or brownfield-maintenance work.

## Coverage (every slice type)

Start with plain-call specs for pure decisions: `Handle()`, `Handle(providedValue)`, reducers and reactor logic. Add in-process scenarios where the pipeline, validation, constraints or projections contribute proof: `CommandScenario<T>` (command pipeline), `EventScenario` (constraints), `ReadModelScenario<T>` (projections/reducers), `ReactorScenario<T>` (reactor invocation). Reserve out-of-process Chronicle integration specs for host/transport boundaries.

0. **Every contract specification first**, one spec each, named after it, using the contract's example values (fresh values only where uniqueness requires). Then add the code-derived cases below that the contract lacks, and report them as proposals for the contract (the `.play` model when one covers the slice, otherwise the agreed outline); never leave them as silent coverage.
1. Happy path with each appended event asserted.
2. One spec per validator rule, asserting **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`. Build the command valid in every other respect so the spec violates only the rule it is named after.
3. One spec per constraint (`ShouldHaveConstraintViolationFor(name)`); authorization via `ShouldNotBeAuthorized()`.

Wrap spec files in `#if DEBUG` only when they compile into the application assembly; dedicated spec projects need no wrapper. Run the specs and fix failures before completing. The skill carries the detail; don't duplicate it here.
