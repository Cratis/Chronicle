<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/baseline-comparison.md -->
# Baseline comparison

For comparison with `main`, verify the registered baseline worktree's commit and
model folder, then use `semantic-diff` with a before path and active after workspace,
not two exports. Read model-authoring
[comparison sources](../../cratis-screenplay-model-authoring/references/mcp-tools.md#semantic-revision-comparison)
when comparing models: source selection, missing-path caveat, identity matching,
revision-pinned paging and drift refusals. Use `read-proposal view=semantic-diff`
for an unapplied proposal against disk. Neither proves behavioral equivalence.

## Comparing with a baseline

For comparison with `main`, use `semantic-diff` with a before path and the active workspace, and
`read-proposal view=semantic-diff` for an unapplied proposal. Neither proves behavioral
equivalence. Source selection, the baseline worktree check, identity matching and drift refusals:
[baseline comparison](baseline-comparison.md).
