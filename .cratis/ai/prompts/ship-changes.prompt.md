---
agent: agent
description: >
  Ship local changes: create a branch, make logical commits, push, open and
  label a PR with a proper description, merge it, prepare related issue
  dispositions the request does not authorize, and delete the branch.
---
<!-- cratis-ai-managed: prompts/ship-changes.prompt.md -->

# Ship Changes

Ship the current local modifications to `main` through the standard
branch → commits → PR → merge → issue disposition → cleanup workflow.

## Inputs

- **What changed** — brief description of the work (used for branch name and PR title)
- **Label** — exactly one of `no-release`, `patch`, `minor`, or `major`, selected from the repository's release contract and applied in `gh pr create --label <intent> --assignee @me`
- **Related issue** — optional exact repository and issue number; if unknown, search read-only first. Comment on or close it only when the user's request includes that effect; otherwise prepare the disposition as a proposal. In repositories released by `cratis/release-action`, a delivered issue is closed by release-action through `(#n)` in the description, but release-action does not run for a `no-release` PR, so `(#n)` closes nothing there; elsewhere follow the repository's own release and issue-closing process. Keep the issue current and close it with a reason per `cratis-engineering-issue-hygiene`.

Invoking this prompt is direct authority for the standard branch, commit, push, pull-request,
requested-label, merge, and branch-cleanup effects. Do not pause to ask for separate approval at
each step. Follow the repository's Git commit and pull-request rules, use a true merge commit,
and verify required checks before merging.

**Exception: a `major` label does not carry merge authority.** Prepare the branch, commits, push,
and PR labeled `major` as usual, then stop before merging. Ask a human to confirm the breaking
change(s), who is affected, and the resulting version number; the human merges it after an
explicit yes to that specific question, and an agent never does — see
[`pull-requests.md`](../skills/cratis-commits-and-pull-requests/references/pr-labels-and-release.md#a-major-release-needs-a-humans-explicit-go-ahead).
Every other label proceeds through merge under this prompt's ordinary authority.

This prompt is for an explicit request to ship or land. If the user asked only to commit, only to
push, or only to open a PR, stop after that step: the narrower request does not become authority
for the rest of the chain because this prompt happens to be loaded.

## Before you branch

Check for an open release candidate or PR in the repository before opening a new branch.
Group related work onto one branch and one PR, and join an open candidate instead of starting
another. The method is in the `cratis-engineering-release-trains` skill.

## Waiting on checks and merging

Verify locally and open the PR once: CI is the final confirmation, not a feedback loop, so every
gate runs on this machine first, with parallelism capped when other agents or builds share it
(see `verification-discipline.md`). Do not open the PR early or keep pushing to it; each push
starts checks on shared runners. Assign the PR to the user you run for with `--assignee @me`.
When this prompt is run by an orchestrator or for sub-agent deliveries, the orchestrator opens the one PR.

Push only when the ship bar is met: root cause understood, specs added, local gate green,
review passed, issue and PR re-read, and no overlap with main. Then watch the exact run once
instead of polling it. Merge only after required checks pass. The method is in the
`cratis-engineering-ci-gates` skill. For review rounds, see `cratis-engineering-review-rounds`.

## Pull request description

The description is published verbatim as the release notes, so it follows the contract in
[`pull-requests.md`](../rules/pull-requests.md#the-description-is-the-release-note).

- Before `gh pr create` or `gh pr edit`, write the body from `.github/pull_request_template.md` and
  save it to `.ai-work/pr-body.md`, and run `node .cratis/ai/hooks/scripts/cratis-check-pr.mjs --body-file .ai-work/pr-body.md --label <intent>`.
  After a merge or rebase from main and before a body edit, reconcile every bullet with
  `git diff --stat origin/main...HEAD`. The rules agents most often break:
  - No development write-up: no Overview/Verification/Test plan headings (only a first `## Summary`
    is allowed), no review, testing or provenance notes. Those go in a PR comment.
  - `(#n)` ends the bullet that delivers an issue and `(part of #n)` marks anything that stays open;
    never `Closes`/`Fixes`/`Refs` before a number, and never an issue reference inside an HTML comment
    (release-action removes comments, so it would close nothing).
  - Links are absolute `https://` URLs, never relative paths.
- Post reviewer notes (checks run, what was not verified, review provenance, stacking, merge or
  deploy order) right after creating the PR with `gh pr comment <n> --body-file .ai-work/pr-notes.md`.
  Never put them in the description, even for `no-release`.
- After pushing, if `policy / verify` fails on the release notes, fix it by editing the description; it re-runs on edit.

## Cleanup — the last step, never skipped

Shipping is not finished until the delivered work is cleaned up. Worktrees and branches left behind
cost real disk and accumulate across sessions. After the merge succeeds:

1. Leave the worktree you worked in: `cd` to the main checkout (`git worktree list` shows it first).
2. Run `bash .cratis/ai/hooks/scripts/cratis-cleanup-worktrees.sh` and report its summary line.
   It fetches, checks out nothing, and removes only worktrees and local branches whose work is
   verifiably on the default branch; it never forces and reports what it kept and why. Worktrees holding
   non-empty `.ai-work/` directories are kept by default. Preserve any needed records before a manual
   `--include-work-records` opt-in; automatic hooks never opt in. Use `git worktree lock <path>` to pin a
   long-lived worktree; locked worktrees remain kept even with opt-in.
3. `git switch main && git pull --ff-only` in the main checkout so it matches the merged result.

Claude Code and Pi also run the script from a hook after `gh pr merge` and at session start; running it
here as well is deliberate, because Copilot, Cursor and OpenCode have no hook and this step is their only
trigger. If the script reports the shipped worktree as kept, say why in the final message.
