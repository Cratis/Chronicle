---
agent: agent
description: Add a Chronicle reactor (an automation, or the adapter side of a translation) that reacts to events and triggers side effects.
---
<!-- cratis-ai-managed: prompts/add-reactor.prompt.md -->

# Add a Reactor

Add a reactor that observes events and produces side effects. Invoke the **cratis-chronicle-reactor** skill and follow `.cratis/ai/rules/reactors.md`.

## Confirm first

- **Model first:** if an accepted model under the model root covers this scope, or the repository is opted in (the model root (default `.cratis/screenplay/`) holds a committed `.play` file (`git ls-tree -r --name-only HEAD -- <root>` lists it) or the project explicitly set `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install output, an installed skill, a `.play` file outside the root or an untracked or uncommitted draft is not opt-in (master: `cratis-screenplay-modeling-lifecycle`)), the automation belongs in the model (`produces` / `invokes`); check there before writing code and follow the model-first routing in `.cratis/ai/rules/general.md`. Reactor code is then gap-fill against the modeled contract. If not opted in, stay code-first.
- **Events to react to**, the **side effect**, and the slice type: `Automation` (our own event causes an external side effect, a follow-up event, or a follow-up command) or `Translation` (data from an outside system recorded as our own facts).
- **Contract details to settle:** where each output field comes from (the trigger event, an injected read model, or a stated mapping), every condition that skips an event, and what must not happen twice.

## Key rules

- `IReactor` is a marker interface; dispatch is by the first parameter type; the method name is descriptive only.
- Reactors are **idempotent** and **stateless**; use event data directly (don't query the read model back).
- To change state elsewhere, return side-effect events or inject `ICommandPipeline` — **never** `IEventLog`.
- `[OnceOnly]` on any non-idempotent side effect (emails, payments, external writes).
- Test with `ReactorScenario<TReactor>`, including a case for each skip condition and one for repeated delivery.

The skill carries the detail; don't duplicate it here.
