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
- **Label** — `patch`, `minor`, or `major`, or omit entirely if no label should be applied
- **Related issue** — optional exact repository and issue number; if unknown, search read-only first. Prepare a post-merge disposition, but do not comment on or close an issue without a separately accepted exact operation profile

Load and follow the full instructions from the `ship-changes` skill.
