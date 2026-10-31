---
agent: agent
description: Add a Chronicle projection to an existing read model slice.
---
<!-- cratis-ai-managed: prompts/add-projection.prompt.md -->

# Add a Projection

Add a Chronicle projection that populates a read model from events. Invoke the **cratis-chronicle-projection** skill and follow `.cratis/ai/rules/vertical-slices.md` (projections). For reactors, use the **add-reactor** prompt instead.

## Confirm first

- **Events to project from** and the **read model** shape.

## Key rules

- Start with **model-bound attributes** on the read model (`[FromEvent<T>]`, `[SetFrom<T>]`, `[Key]`, `[ChildrenFrom<T>]`, `[RemovedWith<T>]`); use fluent `IProjectionFor<T>` when mappings need structure or an attribute-free model. Choose reducer code when prior-state branches, calculations or guards read better as code: default new .NET reducers to static `Reduce` on the model, not a mandatory separate class. Follow **cratis-chronicle-read-model** for the ladder and **cratis-chronicle-reducer** for placement/signatures.
- Preserve admission/ordering guards when converting a reducer, and remove competing self-model builders.
- **AutoMap is on by default — never call `.AutoMap()`** (matching names map automatically).
- Projections consume Chronicle **events**, never other read models.

Run a clean build afterward. The skill carries the detail; don't duplicate it here.
