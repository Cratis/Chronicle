---
name: cratis-commits-and-pull-requests
description: Use before writing commit messages, staging, pushing or creating and editing PRs, including release labels and issue references. Not for candidate assembly, issue status or batching release windows.
license: LICENSE
compatibility: Requires git and the GitHub CLI (gh).
---
<!-- cratis-ai-managed: skills/cratis-commits-and-pull-requests/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Commits and pull requests

Read [git-commits.md](../../rules/git-commits.md) and
[pull-requests.md](../../rules/pull-requests.md) before acting. Their always-on
contracts govern history, release notes, labels, authority and required gates.
Loading this skill grants no permission to push, publish, merge or mutate issues.

1. Inspect the diff and the repository's workflow before choosing the logical
   commit boundaries and release intent. Read [references/commits.md](references/commits.md)
   when staging, writing a message or deciding when to commit. Stage specific
   files and inspect `git diff --cached`; never commit an unbuildable unit.
2. Read [references/pr-description.md](references/pr-description.md) when writing
   or editing a PR or manual release note. Write consumer-facing bullets, not a
   work report. Only fully delivered issues get a closing delivery reference.
3. Read [references/pr-labels-and-release.md](references/pr-labels-and-release.md)
   when choosing or swapping a label, grouping related work, checking gates or
   handling a release-action window. Confirm publication effects before applying
   the single label. A human confirms and merges a major release.
4. Run affected-project gates, then the repository-required checks. Before each
   body edit, compare `git diff --stat origin/main...HEAD` and remove unsupported
   bullets. Validate the description with
   `node .cratis/ai/hooks/scripts/cratis-check-pr.mjs --body-file <file> --label <intent>`.
   A zero exit status is the pass signal; fix the description on failure.
5. Create or edit only when authorized. Put verification, review and deployment
   notes in a PR comment, not the published release note. Confirm the applied
   label and body after writing. Never replace history for a tidier PR.

Minimal description for a delivered consumer fix without an issue reference:

```markdown
## Fixed

- Preserve command validation messages when a request is retried.
```

If the diff only changes non-outward documentation or tooling, choose
`no-release` rather than manufacturing a patch release. If the workflow or
impact is unclear, ask about that specific choice rather than guessing.
