---
agent: agent
description: Add a Chronicle reactor (automation or translation) that reacts to events and triggers side effects.
---

# Add a Reactor

Add a reactor that observes events and produces side effects. Invoke the **add-reactor** skill and follow `.ai/rules/reactors.md`.

## Confirm first

- **Events to react to**, the **side effect / automation**, and whether it's `Automation` (side effects) or `Translation` (triggers commands in another slice).

## Key rules

- `IReactor` is a marker interface; dispatch is by the first parameter type; the method name is descriptive only.
- Reactors are **idempotent** and **stateless**; use event data directly (don't query the read model back).
- To change state elsewhere, return side-effect events or inject `ICommandPipeline` — **never** `IEventLog`.
- `[OnceOnly]` on any non-idempotent side effect (emails, payments, external writes).
- Test with `ReactorScenario<TReactor>`.

The skill carries the detail; don't duplicate it here.
