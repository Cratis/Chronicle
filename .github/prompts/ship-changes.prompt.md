---
agent: agent
description: >
  Ship local changes: create a branch, make logical commits, push, open and
  label a PR with a proper description, merge it, and delete the branch.
---

# Ship Changes

Ship the current local modifications to `main` through the standard
branch → commits → PR → merge → cleanup workflow.

## Inputs

- **What changed** — brief description of the work (used for branch name and PR title)
- **Label** — `patch`, `minor`, or `major` (based on semantic versioning impact)
- **Related issue** — optional GitHub issue number; if unknown, search first

Load and follow the full instructions from the `ship-changes` skill.
