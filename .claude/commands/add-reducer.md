---
agent: agent
description: Add a Chronicle reducer to a read model when model-bound and fluent projections cannot express the state transition.
---

# Add a Reducer

Add an `IReducerFor<T>` reducer — the last-resort escape hatch for a "current state + event → next state" transition that model-bound projection attributes and fluent `IProjectionFor<T>` cannot express. Invoke the **add-reducer** skill and follow `.ai/rules/vertical-slices.md`. For ordinary projections, use the **add-projection** prompt instead.

## Confirm first

- **Why a reducer** (which model-bound / fluent approach was ruled out) and the **events** + **read model** shape.

## Key rules

- Reducers are the **last resort** — exhaust model-bound attributes and fluent `IProjectionFor<T>` first.
- Handle the **nullable current** state (the first event has no prior state).
- Keep reducers passive and deterministic — no side effects, no reading other read models.

Run a clean build afterward. The skill carries the detail; don't duplicate it here.
