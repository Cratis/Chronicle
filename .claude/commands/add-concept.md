---
agent: agent
description: Create a strongly-typed Concept for a primitive domain value or an event-source identity.
---

# Add a Concept

Create a strongly-typed concept to replace a raw primitive. Invoke the **add-concept** skill and follow `.ai/rules/concepts.md`.

## Confirm first

- **Concept name** (e.g. `ProjectId`, `AuthorName`)
- **Underlying primitive** (`Guid`, `string`, `int`, …)
- **Value or identity?** — a **value** concept derives from `ConceptAs<T>`; an **event-source identity** derives from `EventSourceId<T>` (never `ConceptAs<Guid>` — the base already supplies the `EventSourceId`/`T`/`string` conversions).
- **Placement** — the folder that owns it (slice → feature → `Common/`); never a dedicated `Concepts/` folder.

The skill carries the templates and checklist; don't duplicate them here.
