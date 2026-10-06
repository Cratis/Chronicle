---
agent: agent
description: Scaffold a complete vertical slice (contract, backend, specs, frontend) for a Cratis-based project, model-first when a model exists.
---
<!-- cratis-ai-managed: prompts/new-vertical-slice.prompt.md -->

# New Vertical Slice

Implement a complete **vertical slice** end-to-end. Follow the Implementation Workflow in `.cratis/ai/rules/application-profile.md` exactly; for a full backend + specs + frontend slice you may hand the work to the **Slice Implementer** agent. Keep the proportional-delegation policy: a trivial change does not need a multi-agent hierarchy.

## Phase 0 — Find the contract first

Before any slice code, find what the code must match. Decide the route once:

1. **The slice is in an accepted Screenplay model** (search the model root for the slice, command, event and read-model names; default `.cratis/screenplay/**/*.play`, or the configured root). The `.play` slice and its specifications are the contract. Change behavior in the model first (the `cratis-screenplay-*` skills), then render it (`cratis-stage-rendering-and-sandbox`). Hand-write C# only for scope the renderer rejects, or for adapters, infrastructure and clients; the model stays the oracle for that code (`cratis-application-slice-conformance`). Never hand-edit rendered, managed output, and never change the model to match existing code.
2. **The repository is opted in, but the slice is not modeled** (opted in means the model root (default `.cratis/screenplay/`) holds a committed `.play` file (`git ls-tree -r --name-only HEAD -- <root>` lists it) or the project explicitly set `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install output, an installed skill, a `.play` file outside the root or an untracked or uncommitted draft is not opt-in (master: `cratis-screenplay-modeling-lifecycle`); an accepted model elsewhere that does not cover this slice does not by itself opt in): model it first, then continue with route 1. Write code first only when the user chooses to, or the behavior cannot be expressed in `.play`; say then that the model lags the code.
3. **No accepted model covers this scope and the repository is not opted in:** stay code-first and never force a model. Only the entry-point session may propose one, at most once per session, never for trivial, bug-fix, infrastructure, client, framework or brownfield-maintenance work, and not again if declined. Framework, brownfield, infrastructure, client and adapter work keep their code-first path. For new behavior or unclear event vocabulary, run **cratis-chronicle-event-modeling** first; the agreed outline (fields, events, rules, scenarios) is the contract, recorded where the team tracks work, if anywhere.

Whether the Screenplay skills are installed is a separate question from consent: if the route needs them and they are missing, say so and ask; do not author `.play` from memory.

## Confirm first

- **Contract** and where it lives (`.play` slice, or the agreed outline), including its specification list
- **Module / Feature** and **slice name**
- **Slice type**: `State Change` / `State View` / `Automation` / `Translation`
- **Behavior** in one sentence, plus the command/query properties and their concept types (take them from the contract when there is one)

## How it runs

Contract → backend (hand-written parts only) → build (Debug + Release) → specs (the in-process `*Scenario` family, one per contract specification) → frontend → compose/route, with each quality gate green before the next phase. Before reporting done, check that every contract element is realized and nothing is invented (`cratis-application-slice-conformance`). The authoritative rules are `.cratis/ai/rules/general.md` and `.cratis/ai/rules/vertical-slices.md`; the rules carry the step-by-step detail. Do not duplicate that detail here.
