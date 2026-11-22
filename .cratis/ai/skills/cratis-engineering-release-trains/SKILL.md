---
name: cratis-engineering-release-trains
description: Use when grouping finished branches into a release candidate, folding superseded PRs or checking the assembled ship bar. Not for CI waiting, review-round scheduling or workflow optimization.
license: LICENSE
compatibility: Requires git and the GitHub CLI (gh).
---
<!-- cratis-ai-managed: skills/cratis-engineering-release-trains/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# One assembled candidate, one release

A **candidate** is the branch assembling deliveries intended to ship together.
A repository includes its main checkout and all its worktrees. Consider their
concurrent work together, not as separate releases merely because it has
separate branches. One person or session assembles the candidate. Even a single
item ships as a train: the train is the PR, and item branches get no PR.

## Authority and routing

Read [capability-is-not-authority.md](../../rules/capability-is-not-authority.md)
and the [ship-changes prompt](../../prompts/ship-changes.prompt.md) before any
external effect. A user's ship request authorizes its ordinary shipping steps;
loading this skill does not turn an implementation-only request into shipping.
Without that request, prepare the grouping and report the proposed next action.

Read [pull-requests.md](../../rules/pull-requests.md) for release intent, split
criteria, release notes, issue dispositions, and the human checkpoint for a
major release. A major release is never merged by an agent: a human confirms
and merges it. Agent work stops at preparing the PR, even after confirmation.
Read [git-commits.md](../../rules/git-commits.md) before assembling
branches. Preserve commits: never squash, rebase, amend, or force-push.

Use `cratis-engineering-consolidation-guard` when folding existing PRs to prove
nothing was dropped. Use `cratis-engineering-ci-gates` for gate selection and
run handling, and `cratis-engineering-review-rounds` for proportionate review.

## Open the PR once

Each push to an open PR starts checks on shared runners. Push the branch for
backup or hand-off if needed, but open the PR only once, when the work is
verified locally and ready. Push to it again only for a correction that review
or CI found. Do not open a draft PR to see what CI says. All work goes into
the repository's one train PR (see below and `cratis-engineering-consolidation-guard`). An orchestrator
opens the one PR; a sub-agent delivers a verified branch and does not open or
push to a PR unless the request says so. Pass `--assignee @me` when creating it.

## Choose the candidate before creating a PR

1. Inspect the repository's worktrees, finished branches, and open PRs:

   ```bash
   git worktree list
   gh pr list --state open --json number,headRefName,title,author
   ```

2. Group ready deliveries under one candidate and one PR. Check with
   the person assembling an existing candidate before changing it. Independent
   work branches start from `origin/main`; work that depends on an unmerged
   candidate may start from that candidate's latest commit (see Replace waits
   with local work). Ongoing work keeps its branch.
   Do not edit another session's checkout or include its uncommitted work.
3. Prefer joining a related open PR that is still accepting deliveries, has no
   checks running, and is not being merged by another session. Agree on its
   writer, merge `origin/main` into its branch, then merge the deliveries and
   extend its release note. Do not open a competing PR while it is being cut.
4. If no candidate exists, create a branch from current `origin/main` and merge
   ready delivery branches with merge commits. Keep each logical change in its
   own commits. Return a conflicting or failing delivery for correction rather
   than guessing its intent in the assembled branch.
5. Keep one train per repository. Split only for a named major awaiting human
   confirmation, an urgent security fix authorized in writing, or an accepted
   issue reported by someone outside the organization (patch or minor), which
   ships promptly on its own release right after private security work; issues
   captured by internal bots are not outsider issues. Name the exception in the
   reviewer comment. Other unrelated or differently risky work rides a later
   train, not its own PR. Item branches get no PR, CI or
   `run-integration` label; they get local gates and a delta review.

Minimal assembly, after agreeing on the included deliveries:

```bash
git fetch origin
git switch -c release-candidate origin/main
git merge --no-ff delivery-a
git merge --no-ff delivery-b
```

Use a new branch name if `release-candidate` already exists. This example does
not authorize publication. Run the assembled gates before the shipping push.

## Fold superseded PRs promptly

Once a fold is decided within the authorized shipping scope, stop pushing to
its old branch. Close the superseded PR with a one-line reason:
`Folded into <candidate branch or PR>`; use `Will be folded into <candidate>`
when assembly is still pending. Do not wait for the candidate's push to retire
a PR that is already known to be superseded.

Move its delivered release-note bullets into the candidate's release note,
following `pull-requests.md`; retain only issues actually delivered by the diff.
Before consolidation, check release intent and publication effects and apply
`cratis-engineering-consolidation-guard`.

Identify the exact queued or in-progress runs for the superseded head. Cancel
only runs started by this session's own superseded pushes. Never cancel a
release, publish, or deploy run, or another session's watched merge. Leave
others running and report them. Follow `cratis-engineering-ci-gates` for the
cancellation checks; folding a PR is not blanket cancellation authority.

## Cadence, then freeze

A train holds at most about 10 items, or one review slot of glue. Cut the next
train as soon as the previous one merges, or at least every 4 hours while ready
items wait. Never hold the cut for work that is not yet ready; keep items blocked
on a design decision, consumer proof, or another repository out.

After the cut, nothing joins. Late deliveries go into the next candidate.
Corrections needed to make the frozen candidate pass are not new deliveries;
rerun the affected required gates for that changed code state.

Record the cut in one line in the reviewer comment, not the release note:
`Cut with A and B; C waits for consumer proof.` Keep local
coordination records under `.ai-work/`, never in public release text.

## Replace waits with local work

Most waits are removable. The build queue is the only legitimate one; while
queued, do the next independent piece of work.

- Base work on a committed local candidate instead of waiting for it to reach
  main, and say which candidate commit it is based on when delivering.
- Prove against locally built packages instead of waiting for a release; see
  [cratis-engineering-local-cross-repo-verification](../cratis-engineering-local-cross-repo-verification/SKILL.md).
  Only the final version pin waits for the release.
- Evict a stuck or failing item instead of holding the train (next section).
- Cut a train at least every 4 hours with whatever is green.

## Evict, don't hold

If train CI or the glue review fails and the failure is attributable to one
item, revert that item out of the train with a revert commit (never rewrite
history), push once, ship the rest, and send the item to the next train after
its fix. Diagnose an unattributable failure; never evict the whole train blindly.
Merge as soon as CI is green and the review passed (majors stay with a human),
then cut the next train. A train's gate and CI run on the real merged state.

## Apply the ship bar

Check these six conditions. If one is missing, stop and report it instead of
making a speculative shipping push. Recheck the merge conditions after CI.

1. **Cause and design understood.** The change solves the named problem using
   established conventions. Product questions are resolved, not guessed.
2. **Behavior proved.** Changed behavior has specifications, or an explicit
   reason why they do not apply. The assembled local gate is green. Required
   integration coverage follows repository policy and must pass before merge;
   name anything that cannot run locally rather than calling it a local pass.
3. **Reviewed.** Each item head is certified in the review log and the
   train's glue review (merge resolutions, fix-ups, main-merge conflict
   resolutions) completed without Blockers or missing required evidence. A
   whole-change review is needed only for an item without a review-log entry.
4. **Release decision settled.** Release intent follows `pull-requests.md`.
   For a major release, require explicit human confirmation and human merge.
   An agent never merges it; preparing the PR does not answer that checkpoint.
5. **Conversation reread.** Re-read the issues and PR, including comments and
   requested changes, before pushing and again immediately before merging.
   Resolve unanswered objections rather than treating green checks as consent.
6. **Main overlap checked.** Fetch main before merging. Inspect changes since
   the tested head on the candidate's changed paths, for example
   `git log <ci-sha>..origin/main -- <changed-paths>`. If main overlaps, merge it
   into the candidate and rerun the required gates and CI before merge.

## Push deliberately and finish

Batch local commits and review corrections into one meaningful shipping push.
After feedback, fix the full set, rerun the required gates, and push once.
Never push a lone merge of main as a save point or push just to discover whether
untested code passes.

Never put a CI skip token (`[skip ci]`, `[ci skip]`, `[no ci]`, `[skip actions]`)
in a PR title or commit message. The merge commit inherits the PR title, so a
skip token can prevent every check from running on main. Remove such tokens
before creating or editing the PR; never rewrite an existing commit to do it.
If a committed token cannot be corrected without rewriting history, stop and
report the unsafe message rather than pushing it.

Use the ship-changes prompt for PR creation, notes, merge, issue disposition,
and cleanup. Non-major releases use a merge commit; never squash or rebase.
A human confirms and merges every major release. In repositories
using release-action, delivered `(#n)` references close at release, not by hand;
follow `pull-requests.md` for other cases rather than inventing a closing step.

Do not enable auto-merge. Wait using `cratis-engineering-ci-gates`. If checks
outlast the session, leave a handoff under `.ai-work/` per
[local-work-artifacts.md](../../rules/local-work-artifacts.md), with valid
preconditions, candidate and tested SHAs, PR, exact pending checks, and the next
steps. The next session must recheck the ship bar, not blindly merge on green.

## What breaks

- **A PR per item or a delivery outside the train:** duplicate gates and fragmented releases replace an
  assembled check of the actual shipped change.
- **A growing frozen candidate:** late work invalidates the gate and cut.
- **Green means permitted:** checks neither resolve objections nor approve a
  major release. The user's request and owning rules still govern effects.

## How it is proven

Run the repository's exact assembled gate and inspect its successful exit.
Run `gh pr checks <pr>` before merge: required checks must pass for the tested
head. Confirm the review-log-certified items and glue review, fresh discussion, and main-overlap check.
For release notes, execute the repository's `cratis-check-pr.mjs` command from
`pull-requests.md`; a zero exit is the pass signal. No checklist replaces those
behavioral and CI signals.
