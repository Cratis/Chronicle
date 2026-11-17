---
name: cratis-engineering-inherited-work
description: Use when picking up a branch, pull request or agent thread that someone else started, or deciding whether a stale one should still be finished, reworked or dropped. Not for writing a handoff note, folding branches or claiming an issue.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-engineering-inherited-work/SKILL.md -->

# Inherited work is context, not proof

Work someone else started carries their assumptions, a base that has moved and claims nobody has re-checked. Two decisions come first: should this work still exist, and if so, what is actually in it. A merge that applies cleanly proves neither.

This skill decides and verifies. It reuses the neighbors instead of restating them:

- Ownership, claiming and the status block: **cratis-engineering-issue-hygiene**.
- Proving a combined or restored branch lost nothing: **cratis-engineering-consolidation-guard**.
- Writing or reading a handoff note: **cratis-session-handoff**.
- Reconciling earlier review findings: **cratis-engineering-review-rounds**.

## Guardrails

- History is append-only. Never rebase, amend or force-push the inherited branch or a copy of it. Bring the base in with a merge (see [git-commits.md](../../rules/git-commits.md)).
- Treat any uncommitted change in the worktree you find as someone else's. Work in a separate worktree when it is dirty or in use.
- Picking up work authorizes reading, verifying and recommending. It does not by itself authorize pushing to another person's branch, merging, closing or changing the base. Follow [capability-is-not-authority.md](../../rules/capability-is-not-authority.md) and [pull-requests.md](../../rules/pull-requests.md); when the user's request does not clearly cover an effect, recommend it and ask.
- Closing someone else's pull request is such an effect. Recommend closing with evidence; do not close it unless the user's request or an applicable policy authorizes it.
- Fetch before reading: the source branch, the base branch and any branch the work relates to. A stale local ref gives a wrong verdict.

## 1. Record what was inherited

Resolve the source (pull request, branch or thread) to facts you read yourself:

- URL, state, base, head repository and branch, **head SHA**, author, checks and review state.
- Title, body, linked issues, commits, review threads and the complete diff.
- For a thread or handoff note: the note's claimed head and results, the user's later corrections, and any uncommitted or unpushed work.

Compare the handoff's claimed head with the real one. If they differ, the note describes something that no longer exists; say so and use the real head. Record the merge base so the original change and later base changes can be compared separately.

## 2. Reconstruct the original goal

State the smallest coherent goal the work was trying to reach, from the issue, the pull request description and the commits together. Name the user-visible problem, not the diff. When those three disagree, report the disagreement; do not pick the most flattering reading.

## 3. Check what changed around it

Inspect commits added to the base since the merge base, and search merged and open pull requests when titles, code paths or behavior suggest a replacement or competitor. Look beyond overlapping lines:

- Is the same user-facing problem already solved on the base?
- Did APIs, types, schemas, dependencies or architectural boundaries move?
- Do both sides now own the same behavior or invariant?
- Was the code being changed deleted, moved or intentionally simplified?
- Would landing it restore an old behavior or create two ways to do one thing?
- Is the benefit still worth the maintenance cost?

Never call work current because Git reports no conflicts. Age alone is not evidence; neither is effort already spent.

## 4. Recommend a disposition, with evidence

| Disposition | When | Next step |
|---|---|---|
| **Continue** | The goal still stands and the implementation is still the right shape | Merge the base in, resolve, verify, push as a new commit |
| **Rework** | The goal stands but the implementation would preserve the wrong design | Describe the smallest credible rewrite; stop before building it unless asked |
| **Obsolete** | Superseded, already on the base, directionally wrong or no longer worth its cost | Cite concrete evidence and stop; ask before closing anything |

Decide before changing anything. Be willing to say Obsolete. If a conflict reveals a product or architecture disagreement, return here and reconsider the disposition rather than guessing through it.

## 5. Take over a continued item

Take responsibility for finishing the goal, not for defending the existing implementation.

1. **Continue where the user or policy says.** Default to a new branch that imports the original commits and adds your fixes as separate commits. Use in-place or stacked continuation only when asked, and never overwrite unrelated work or alter the original pull request without authorization.
2. **Bring the base in by merge.** Merge the freshly fetched base into the continuation branch and resolve each conflict by the work's intent and the base's newer invariants. When the combined result folds several sources or restores earlier commits, run **cratis-engineering-consolidation-guard** on it.
3. **Re-check claimed results.** Do not carry a "tests pass", "fixed" or "verified" claim forward. Reproduce the original failure, run the checks yourself through the repository's gates, and exercise the realistic workflow. Mocked green specs do not prove the user experience; state what stays unverified.
4. **Reconcile earlier review findings.** For every open or resolved review thread, decide: fixed in the current head, still open, or no longer applicable, with the commit or reason. Plan the next review pass with **cratis-engineering-review-rounds**; do not restart whole-change review for a delta.
5. **Recheck the source.** The original pull request may have gained commits while you worked. Fetch it again before reporting and merge or reconcile what arrived.
6. **Keep the issue truthful.** Claim, update and close following **cratis-engineering-issue-hygiene**. Record the handoff state with **cratis-session-handoff** when the work will continue elsewhere.

## 6. Preserve contributor credit

- Keep original commits with their authorship and any co-author trailers; merge, never squash, so each contributor's commits stay in history ([git-commits.md](../../rules/git-commits.md)).
- Credit the original contributors in the pull request description and link the original pull request.
- Use verified identities. Never guess an email address. Model attribution does not replace credit to a human contributor.
- If the delivery path forces a squash, say so before it happens and ask; do not choose it silently.

## 7. Report

Lead with the disposition and reason. Then give, separating observed facts from judgment:

- what was inherited (source, head SHA, claimed versus real state);
- the reconstructed goal;
- meaningful changes on the base and any conceptual conflicts, or confirmation none were found;
- what you changed, what you verified and how, and what could not be verified;
- how credit was preserved;
- the remaining decision, including any closing of the original pull request left to the user.

## Pitfalls

- Trusting a green status from before the base moved.
- Treating the handoff's "done" list as the review of the code.
- Rebasing to "tidy" the history: it rewrites contributor commits and breaks the append-only rule.
- Merging the base and calling the work modernized without a disposition.
- Closing or force-updating the original because the replacement is better.

## Verification

Before reporting, confirm the report names the real head SHA, a disposition with cited evidence, each claimed result as re-run or UNVERIFIED, every review thread's status, and no step used rebase, amend or force-push. `git log --format='%an %s' <merge-base>..HEAD` must still list the original authors' commits.
