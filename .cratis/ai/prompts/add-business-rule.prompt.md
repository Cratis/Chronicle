---
agent: agent
description: Add a business rule or event-store constraint to an existing command.
---
<!-- cratis-ai-managed: prompts/add-business-rule.prompt.md -->

# Add a Business Rule or Constraint

Enforce a new rule on an existing command. Invoke the **cratis-arc-command-validation** skill, or the **cratis-chronicle-event-constraints** skill for uniqueness and other append-time constraints, and follow `.cratis/ai/rules/vertical-slices.md` (the decision matrix).

## Confirm first

- **Command** to constrain, and the **rule** in one sentence.

## Pick the mechanism

- Reusable value invariant → `ConceptValidator<T>`.
- Command-input / cross-field / pre-handler rule → `CommandValidator<T>`.
- Handler needs fetched data first → `Provide()`.
- State rule that must hold **under concurrency** → enroll a stream decision with `IStreamReads`/`StreamDecision`, or use `[ProtectedDecision]` with an admitted `DecisionRead<T>`; plain read-model injection and `Result<TEvent, ValidationResult>` alone add no guard. Specify the race with `UseDecisionReads()` and seeded events, not pinned models.
- Uniqueness → `[Unique]` / `IConstraint`.

> **Never throw for normal business rejection** — a throw is HTTP 500, not a validation error. Recoverable rejection is a `ValidationResult` / `Result<,>`. Add a spec for the failure case (assert both not-successful and has-validation-errors). The skill carries the detail; don't duplicate it here.
