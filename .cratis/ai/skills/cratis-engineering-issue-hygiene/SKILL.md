---
name: cratis-engineering-issue-hygiene
description: Use when claiming an issue, maintaining its status block or deciding an authorized closure or necessary follow-up. Not for delivery grouping, review rounds or CI operations.
license: LICENSE
compatibility: Requires git and the GitHub CLI (gh).
---
<!-- cratis-ai-managed: skills/cratis-engineering-issue-hygiene/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Leave fewer issues, with a truthful description

An issue must make sense to a reader outside the session. Keep its present state
in the description and meaningful events in the timeline. Before any mutation,
read [capability-is-not-authority.md](../../rules/capability-is-not-authority.md).
The request authorizes its named effects, not arbitrary filing or closing.

## One writer and a clear claim

One person or session owns all body edits, comments and disposition for a named
issue. Delegated workers report findings to that owner. An explicit handoff can
transfer the named issue, but the previous writer must stop editing it. GitHub
body edits are not conditional writes; a last-minute reread alone cannot prevent
two writers from overwriting each other.

Before starting, read the issue, comments, assignees and any status block. Follow
[issue-assignment.md](../../rules/issue-assignment.md) and this claiming procedure:

- Applies when the user, branch, task or handover names an issue number in a
  known repository. No known repository or issue means no guessed assignment.
- Read assignees and state in that issue's repository:
  `gh issue view <n> --repo <owner>/<repo> --json assignees,state`.
- If open and unassigned, run
  `gh issue edit <n> --repo <owner>/<repo> --add-assignee @me`.
  `@me` means the authenticated user, not an invented identity.
- Never replace or add to an existing assignment. Skip a closed issue; say so
  without reopening or assigning it.
- Do this once, at the start before the first edit, not on every turn. If it
  fails (permissions or authentication), report it and continue with the work.

Assign each PR you open the same way: `gh pr create --assignee @me`, or
`gh pr edit <n> --add-assignee @me` afterwards.

The request to work on the issue authorizes this assignment only, not comments,
labels or closure. If another writer is active, coordinate rather than starting
a parallel edit. Delegated workers do not claim or mutate the issue.

When the request covers maintaining the issue, set the status block to
**In progress** before coding. Keep it true as work moves: **In review** for the
open PR, **Blocked** with the dependency, **Paused** when no active work remains,
and **Done** only when delivery is confirmed. Never leave In progress behind
when the writer stops.

## Put the status block in the description

Preserve the original problem statement and all text outside the managed block.
Append the block if absent. Stop for coordination if markers are malformed,
duplicated or already managed by another active writer. A minimal template is:

```markdown
<!-- agent-status:start -->
## Status
**State:** In progress
**Scope:** Deliver the agreed behavior and its regression specification.
**Approach:** Fix the shared decision and exercise it through the public API.
**PR:** Not opened.
**Delivered:** Nothing yet.
**Remaining:** Implement, verify, review and deliver.
**Follow-ups:** None.
<!-- agent-status:end -->
```

Replace the example with the real scope and remaining work. Use public PR or
issue URLs when they exist. Summarize a local plan; never link its file path.
Do not put local paths, harness commands, credentials or private context in
public issue or PR text. Never name a customer or a customer's customer in an
issue, comment or PR; describe the situation generically ("a large enterprise
tenant") and strip identifying products, systems and people. Read [secrets.md](../../rules/secrets.md) before handling
output, and [local-work-artifacts.md](../../rules/local-work-artifacts.md) before
creating the private working copy.

For example, read the body into an ignored local working file:

```bash
gh issue view <n> --repo OWNER/REPO --json body --jq .body > .ai-work/issue-body.md
```

Edit only the managed block in that file. Immediately before writing, re-read
both the issue and any linked PR. Read the remote body again:

```bash
gh issue view <n> --repo OWNER/REPO --json body
```

Compare it with the body used to prepare the edit. If anything changed, preserve
those changes and recompose from the fresh body. If concurrent edits continue,
stop and coordinate with the other writer. Do not write a stale whole body.
After a clean comparison, write the complete preserved body back, for example:

```bash
gh issue edit <n> --repo OWNER/REPO --body-file .ai-work/issue-body.md
gh issue view <n> --repo OWNER/REPO --json body
```

Verify the block changed as intended and the surrounding content stayed intact.
Do not paste the private working-file path into the issue.

## Description versus comment

Edit the status block for current scope, decisions, approach, checklist, linked
PRs, dependencies and what remains. The description alone should explain the
present state. Do not rewrite other people's text or replace the original report.
The one design comment linked from the block is the edited-in-place exception; see **cratis-engineering-issue-design-note**.

Comment only for an event worth notifying readers about: a decision or scope
change with its reason, a human blocker, a delivery milestone or a closing
summary. Each comment stands alone: what happened, why it matters, what comes
next. Use absolute public URLs. Do not repeat the block as a comment or post
"working on it", tool output, test counts, receipts or model/session chatter.

Re-read the issue and PR before every key step: starting, changing scope,
updating public state, filing follow-ups, merging or disposing of the issue.
For example, use `gh issue view <n> --repo OWNER/REPO --comments` and
`gh pr view <pr> --repo OWNER/REPO`. Do not act on an old summary after drift.

## Make the open-issue count go down

1. **Fix before filing.** Fix a small, in-scope finding in the same files and
   risk area as the active change. Do not widen scope to improve the count.
   Review advisories are fixed cheaply inline or dropped, not filed as issues.
2. **File only when needed and authorized.** A real defect that cannot be fixed
   now, or a follow-up delivery truly requires, may need an issue. Search open
   and closed issues first, for example
   `gh issue list --repo OWNER/REPO --state all --search "<area> in:title,body"`.
   Reuse an existing issue instead of duplicating it; comment only when that
   effect is authorized and adds necessary context.
3. Keep every issue generic: no customer or customer-of-customer names, ever.
   Group related findings into one checklist issue per area. Include concrete,
   testable `## Acceptance criteria`. No speculative hardening, ideas or
   "investigate later" issues. If filing is outside the request, list necessary
   follow-ups in the report instead of creating them.
4. **Close what is done**, through the correct delivery mechanism below. Partial
   delivery stays open with exactly what remains. Do not manufacture completion
   or create extra issues merely to move unfinished scope elsewhere.
5. Prefer bounded issues that can be finished completely over starting large
   epics. Report completed dispositions, not how many new issues were produced.

## Close with a reason, through the owning mechanism

Read [pull-requests.md](../../rules/pull-requests.md#issue-references) before
changing issue references or disposition. Confirm the actual PR state, release
intent and repository workflow; a merged PR is not proof of a published release.

- **Release-action delivery:** for a fully delivered issue, the delivering
  release-note bullet ends with `(#n)`. Release-action closes it at release;
  do not close it by hand. If release is pending, keep that dependency visible.
- **Manual completion:** only for a `no-release` PR or a repository without
  release-action, and only when the request includes closing. Confirm all scope
  is delivered and no required PR is open, unmerged, reverted or failing. Set
  Done and close with reason `completed` and a public delivery link.
- **Duplicate or obsolete:** only with clear evidence and authority. Use reason
  `not planned`, explain why, and link the canonical issue or superseding work.
  If uncertain, leave it open and report the question.
- **Partial or unauthorized disposition:** leave it open with truthful remaining
  scope or report the proposed disposition. Do not invent follow-up authority.

For an authorized manual completion, for example:

```bash
gh issue close <n> --repo OWNER/REPO --reason completed \
  --comment "Delivered in <public PR URL>. The agreed behavior is available."
```

A duplicate uses `--reason "not planned"` and a specific canonical public URL.
No closing keywords are needed in the PR description; follow the owning rule.

## End-of-work audit

List issues touched by this work and reconcile each against its PR and release.
For example, `gh issue list --repo OWNER/REPO --assignee @me --state open` helps
find assigned work, but does not replace the list of issues actually touched.
Check merged PRs for completed work still open; distinguish pending release-action
closure from an authorized manual disposition. Verify closure through the issue's
actual state, not an assumption about merging.

Leave no issue In progress without an active writer. For unfinished work, set
Paused or Blocked with what remains and what it waits for. Reconcile follow-ups
with existing issues. Perform only authorized mutations; otherwise report the
proposed changes and the blocker. Summarize completed issues, pending release
closures and unfinished scope without publishing local paths or harness details.

## How it is proven

Read the final body and state, for example with
`gh issue view <n> --repo OWNER/REPO --json body,state`. The original report is
intact, the status block matches the actual PR and release, and any authorized
closure is observable. Until release-action runs, pending closure is not Done.

## What breaks

- Two body writers lose each other's updates even if both reread first.
- A status comment buries current state and needlessly notifies subscribers.
- Manual closure before release bypasses release-action's delivery contract.
- Filing every advisory grows the backlog without a necessary delivery.
- Stale In progress blocks the next person without anyone doing the work.
