---
agent: agent
description: Add a business rule or event-store constraint to an existing command.
---

# Add a Business Rule or Constraint

Enforce a new rule on an existing command. Invoke the **add-business-rule** skill and follow `.ai/rules/vertical-slices.md` (the decision matrix).

## Confirm first

- **Command** to constrain, and the **rule** in one sentence.

## Pick the mechanism

- Reusable value invariant → `ConceptValidator<T>`.
- Command-input / cross-field / pre-handler rule → `CommandValidator<T>`.
- Handler needs fetched data first → `Provide()`.
- State rule that must hold **under concurrency** → inject the read model into `Handle()`, return `Result<TEvent, ValidationResult>`.
- Uniqueness → `[Unique]` / `IConstraint`.

> **Never throw for normal business rejection** — a throw is HTTP 500, not a validation error. Recoverable rejection is a `ValidationResult` / `Result<,>`. Add a spec for the failure case (assert both not-successful and has-validation-errors). The skill carries the detail; don't duplicate it here.
