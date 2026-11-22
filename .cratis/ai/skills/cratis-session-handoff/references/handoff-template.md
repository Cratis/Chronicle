<!-- cratis-ai-managed: skills/cratis-session-handoff/references/handoff-template.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Handoff template

## Contents

- Template
- Rules for filling it in
- Filled example

A handoff lets a fresh session continue work. Save it as
`.ai-work/<task>/handoff.md`; a lane uses `.ai-work/<task>/handoff-<lane-id>.md`.
It opens with the preconditions (repository, branch, expected `HEAD`, worktree
paths, date) required by
[local-work-artifacts.md](../../../rules/local-work-artifacts.md). The next
session compares them with `git` before acting; a mismatch means the handoff is
stale, not that the work is lost.

The structure is fixed so a tool can check it: the headings below appear
exactly as spelled, in this order, and the table headers are not reworded.
Every section is present even when empty; write `none` so an absence is
detectable and never ambiguous.

## Template

```markdown
# Handoff: <task>

**Preconditions:** repository `<repo>`, branch `<branch>`, HEAD `<full-sha>`,
worktrees `<paths>`, written `<date>`.

## Goal
<one sentence>

## Repositories and worktrees
| Path | Repository | Branch | HEAD (full SHA) | State | Ahead/behind |
|---|---|---|---|---|---|
| <path> | <repository> | <branch> | <full-sha> | <clean or dirty> | <ahead/behind> |

## Verified facts (with evidence)
- <fact>: <evidence: command run, SHA, run id>

## Plans and hypotheses (not verified)
- <plan or guess>

## Watch and action
| Id | Target (immutable) | Action on completion | How to cancel |
|---|---|---|---|
| <job, run or subagent id> | <run id, head SHA or job id> | <what to do> | <command or step> |

## Finished, results not yet read
- <job id>: <where the result lives>

## Open questions carried forward
- <question>: <who asked and when>, <status>

## User constraints (verbatim)
> <quoted exactly, never paraphrased>

## User instructions since the last handoff (verbatim)
> <quoted exactly, or none>

## Next commands
1. `<command>`

## Files to read
- `<path>`
```

## Rules for filling it in

- State is `clean` or `dirty`, nothing else. Ahead/behind is relative to the
  branch's upstream.
- Facts and plans stay in separate sections. A claim without evidence is a plan.
- The watch table has one row per running job, CI watch or subagent. The target
  is an immutable identifier (run id, pull request head SHA, job id), never
  "latest" or "the PR", because the thing it points to moves.
- When nothing runs, the watch table keeps one row whose cells say `none`.
- Results that finished but were never read go under their own heading so the
  next session reads them first.
- Quote user words exactly. A paraphrase loses the constraint's edges.

## Filled example

```markdown
# Handoff: export retry cap

**Preconditions:** repository `export-service`, branch `export-retry`, HEAD
`0123456789abcdef0123456789abcdef01234567`, worktrees
`../.worktrees/export-retry` and `../.worktrees/export-docs-retry`, written
2025-01-01.

## Goal
Cap export retries at five and surface the final failure.

## Repositories and worktrees
| Path | Repository | Branch | HEAD (full SHA) | State | Ahead/behind |
|---|---|---|---|---|---|
| ../.worktrees/export-retry | export-service | export-retry | 0123456789abcdef0123456789abcdef01234567 | clean | 2/0 |
| ../.worktrees/export-docs-retry | export-docs | export-retry-docs | 89abcdef0123456789abcdef0123456789abcdef | dirty | 1/0 |

## Verified facts (with evidence)
- Cap and spec committed: `git log -1` shows the SHA above.
- Spec project passes: `./Source/Export.Specs/bin/Debug/net10.0/Export.Specs` exit 0 at that SHA.
- Pull request is open: `gh pr list --head export-retry` shows one entry.

## Plans and hypotheses (not verified)
- The integration job may need the same cap; not inspected.

## Watch and action
| Id | Target (immutable) | Action on completion | How to cancel |
|---|---|---|---|
| ci-1 | CI run `1234567890` on head `0123456789abcdef0123456789abcdef01234567` | fails: read the failing job log, fix in owned files; passes: report ready | `gh run cancel 1234567890` |

## Finished, results not yet read
- review-1: findings are in `.ai-work/export-retry/review-1.md`

## Open questions carried forward
- Should the cap be configurable per tenant: asked by the user in the first session, unanswered.

## User constraints (verbatim)
> Do not change the public job interface.

## User instructions since the last handoff (verbatim)
> none

## Next commands
1. `git rev-parse HEAD` and compare with the preconditions
2. `gh run view 1234567890 --json status,conclusion`

## Files to read
- `Source/Export/ExportJob.cs`
- `.ai-work/export-retry/STATE.md`
```
