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
- **Label** — `no-release`, `patch`, `minor`, or `major`, or omit entirely if no label should be applied
- **Related issue** — optional exact repository and issue number; if unknown, search read-only first. Comment on or close it only when the user's request includes that effect; otherwise prepare the disposition as a proposal. In repositories released by `cratis/release-action`, a delivered issue is closed by release-action through `(#n)` in the description, but release-action does not run for a `no-release` PR, so `(#n)` closes nothing there; elsewhere follow the repository's own release and issue-closing process.

Invoking this prompt is direct authority for the standard branch, commit, push, pull-request,
requested-label, merge, and branch-cleanup effects. Do not pause to ask for separate approval at
each step. Follow the repository's Git commit and pull-request rules, use a true merge commit,
and verify required checks before merging.

**Exception: a `major` label does not carry merge authority.** Prepare the branch, commits, push,
and PR labeled `major` as usual, then stop before merging. Ask a human to confirm the breaking
change(s), who is affected, and the resulting version number, and merge only on an explicit
affirmative answer to that specific question — see
[`pull-requests.md`](../rules/pull-requests.md#a-major-release-needs-a-humans-explicit-go-ahead).
Every other label proceeds through merge under this prompt's ordinary authority.

This prompt is for an explicit request to ship or land. If the user asked only to commit, only to
push, or only to open a PR, stop after that step: the narrower request does not become authority
for the rest of the chain because this prompt happens to be loaded.

## Pull request description

The description is published verbatim as the release notes, so it follows the contract in
[`pull-requests.md`](../rules/pull-requests.md#the-description-is-the-release-note).

- Before `gh pr create` or `gh pr edit`, write the body from `.github/pull_request_template.md` and
  check it against the contract. The rules agents most often break:
  - No development write-up: no Overview/Verification/Test plan headings (only a first `## Summary`
    is allowed), no review, testing or provenance notes. Those go in a PR comment.
  - `(#n)` ends the bullet that delivers an issue and `(part of #n)` marks anything that stays open;
    never `Closes`/`Fixes`/`Refs` before a number, and never an issue reference inside an HTML comment,
    which still closes the issue.
  - Links are absolute `https://` URLs, never relative paths.
- After pushing, if `verify-release-notes` fails, fix it by editing the description; it re-runs on edit.
