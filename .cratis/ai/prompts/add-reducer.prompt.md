---
agent: agent
description: Add a Chronicle reducer to a read model when model-bound and fluent projections cannot express the state transition.
---
<!-- cratis-ai-managed: prompts/add-reducer.prompt.md -->

# Add a Reducer

Add reducer code for a "current state + event → next state" transition that reads better as code. Default new .NET code to a self-reducing model with public static `Reduce` methods; use `IReducerFor<T>` for injected services, replay instance behavior or separation. Invoke the **cratis-chronicle-reducer** skill and follow `.cratis/ai/rules/vertical-slices.md`. For ordinary projections, use the **add-projection** prompt instead.

## Confirm first

- The **state-transition rule**, **events** and **read model** shape; whether fold-time services or separation justify an instance reducer.

## Key rules

- Start with attributes for clear mappings, fluent for structure, reducer code for clearer prior-state branches, calculations or guards — a normal choice, not a last resort.
- Nominate a plain instance class only when its assembly cannot reference Chronicle; keep the other SDK and Stage contracts separate.
- Keep `Reduce` and Arc queries on the model record; apply the skill's versioned query-discovery guidance. Remove any competing builder and preserve admission/ordering guards.
- Handle the **nullable current** state (the first event has no prior state).
- Keep folds deterministic — no side effects, no reading other read models. Passivity is a separate choice (`[Passive]` on the model); it does not make decision reads admit reducers.

Run a clean build afterward. The skill carries the detail; don't duplicate it here.
