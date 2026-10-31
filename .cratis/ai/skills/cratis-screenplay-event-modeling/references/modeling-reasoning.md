<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/modeling-reasoning.md -->
# Keeping modeling reasoning with its declaration

Where assumptions, boundaries and rejected alternatives live in the model.

## Keep modeling reasoning with its declaration

Use `description` for summaries and fenced Markdown `documentation` on modules,
features, slices, commands, read models and reactions (also events) for assumptions,
boundaries and rejected alternatives. Do not leave that reasoning only in comments
or disconnected notes. Both are report-only (PLAY0270), never executable rules.
Specifications use `description` to name the witnessed behavior; longer reasoning
belongs to their slice. Syntax, merge rules and diagnostics:
[model reasoning](../../cratis-screenplay-slice-design/references/model-reasoning.md).
