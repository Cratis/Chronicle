---
agent: agent
description: >
  Ship local changes: create a branch, make logical commits, push, open and
  label a PR with a proper description, merge it, prepare no-effect related
  issue dispositions, and delete the branch.
---
<!-- cratis-ai-managed: prompts/ship-changes.prompt.md -->

# Ship Changes

Ship the current local modifications to `main` through the standard
branch → commits → PR → merge → no-effect issue disposition → cleanup workflow.

## Inputs

- **What changed** — brief description of the work (used for branch name and PR title)
- **Label** — `no-release`, `patch`, `minor`, or `major`, or omit entirely if no label should be applied
- **Related issue** — optional exact repository and issue number; if unknown, search read-only first. Comment on or close it only when the user's request includes that effect.

Invoking this prompt is direct authority for the standard branch, commit, push, pull-request,
requested-label, merge, and branch-cleanup effects. Do not pause to ask for separate approval at
each step. Follow the repository's Git commit and pull-request rules, use a true merge commit,
and verify required checks before merging.
