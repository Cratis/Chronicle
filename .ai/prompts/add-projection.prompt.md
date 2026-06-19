---
agent: agent
description: Add a Chronicle projection to an existing read model slice.
---

# Add a Projection

Add a Chronicle projection that populates a read model from events. Invoke the **add-projection** skill and follow `.ai/rules/vertical-slices.md` (projections). For reactors, use the **add-reactor** prompt instead.

## Confirm first

- **Events to project from** and the **read model** shape.

## Key rules

- Default to **model-bound attributes** on the read model (`[FromEvent<T>]` class-level, `[SetFrom<T>]`, `[Key]`, `[ChildrenFrom<T>]`, `[RemovedWith<T>]`); drop to fluent `IProjectionFor<T>` only for joins/transforms; reducer for "current state + event → next state".
- **AutoMap is on by default — never call `.AutoMap()`** (matching names map automatically).
- Projections consume Chronicle **events**, never other read models.

Run a clean build afterward. The skill carries the detail; don't duplicate it here.
