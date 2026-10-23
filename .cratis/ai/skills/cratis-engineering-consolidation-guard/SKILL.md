---
name: cratis-engineering-consolidation-guard
description: Prove that a consolidation or re-land dropped nothing - compare the result with every original branch or commit, detect lost files that commit pairing cannot see, treat a merge resolution that reverts a file as a finding, ratchet the specification count, and enforce it as a required CI check. Use when folding several pull requests into one branch, re-landing reverted work, restoring commits by cherry-pick, or reviewing such a branch before it merges. Not for an ordinary merge of the base into a feature branch, for pull request descriptions or labels (follow the pull-requests rule), or for general code review (use cratis-code-review).
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-consolidation-guard/SKILL.md -->

# Consolidations and re-lands prove nothing was dropped

A **consolidation** is any branch whose job is to carry changes authored somewhere
else: several pull requests folded into one, reverted work brought back, commits
restored from an abandoned branch. Its **sources** are those original branches
and commits.

The combined tree is built and specified before it merges, and that is exactly
why loss goes unnoticed. A change deleted *by the merge itself* leaves nothing
behind to fail: a deleted specification cannot go red, and a file a conflict
resolution put back to its old content compiles fine. The guard compares the
result with every source, path by path, and turns each loss into a finding.

**A re-land is a consolidation.** Work that landed, was reverted and is landed
again has one source, the original change, and loses files the same way. Run
the same guard against that original change, not against the revert.

## When the guard applies

- Folding two or more pull requests onto one branch, as
  [pull-requests.md](../../rules/pull-requests.md) asks for small related work.
- Re-landing reverted work, whether by reverting the revert, cherry-picking the
  original commits, or merging the original branch again.
- Restoring commits by cherry-pick: from a deleted branch, a closed pull
  request, or a backport.
- Reviewing any of the above before it merges.

It does not apply to merging the base branch into a feature branch (the base is
not a source), or to a fresh implementation that shares no history with the
work it replaces; there, specify the behavior instead.

## Consolidate without rewriting history

[git-commits.md](../../rules/git-commits.md) forbids rebase, squash and force.
Each permitted way to consolidate also records its source in history, which is
what lets CI find the sources without being told:

| Situation | Default | Recorded as |
| --- | --- | --- |
| Fold a pull request | `git merge --no-ff pr631` | a merge whose second parent is the folded tip |
| Restore commits | `git cherry-pick -x <sha>` | a `(cherry picked from commit <sha>)` line |
| Re-land reverted work | `git revert <revert-sha>` | `This reverts commit <revert-sha>.` |

Choose cherry-pick over reverting the revert when only some of the original
commits come back; every change left out is then a finding that needs a
disposition.

## Name the sources

A source is either a ref or a range `<from>..<ref>`. A bare ref means the change
since `merge-base(<base>, <ref>)`; the range means the change since
`merge-base(<from>, <ref>)`.

| Source | Argument |
| --- | --- |
| A folded pull request | `git fetch origin "refs/pull/631/head:pr631"`, then `pr631` |
| A merged pull request being re-landed, merge commit `M` | `M^1..M^2` |
| A single commit `C`, or a pull request that landed as one commit | `C^..C` |
| A branch commits were cherry-picked from | its head ref, or `C^..C` per commit |

Fetch the pull request's own head ref rather than its branch: `refs/pull/<n>/head`
survives the branch being deleted.

Work that already landed needs the range form. Its commits are ancestors of the
base, so `merge-base(<base>, <ref>)` is the tip itself and the comparison would
be empty; the script refuses that with exit `2` rather than reporting a pass.

Confirm a folded branch has not moved since it was fetched. When
`gh pr view 631 --json headRefOid --jq .headRefOid` differs from
`git rev-parse pr631`, fetch again and fold the new commits before verifying.

## Run the guard

Execute [scripts/verify-consolidation.sh](scripts/verify-consolidation.sh) inside
the repository, with the consolidation ref first and every source after it.

Requires git 2.28 or later (`git --version`); with an older git, run the three
checks below by hand.

```bash
guard=<skill-directory>/scripts/verify-consolidation.sh
git fetch origin
for n in 631 640 652; do git fetch origin "refs/pull/$n/head:pr$n"; done
"$guard" consolidate/backlog-filters pr631 pr640 pr652
```

A re-land of a merged pull request, given the original merge commit:

```bash
landed=<original-merge-sha>
"$guard" reland/dashboard-counters "$landed^1..$landed^2"
```

Options: `--base <ref>` (default `origin/main`), `--spec-pattern <regex>` for a
repository whose specifications are not `for_*`/`when_*` folders,
`--allow-rewritten <sha>` and `--allow-spec-delta <n>` for dispositions, and
`--creation-factor <n>` for `git range-diff` pairing.

Before choosing sources by hand, list the ones history records and compare:

```bash
"$guard" --discover consolidate/backlog-filters
```

A source on one list and not the other is a finding of its own: either a fold
that was not intended, or an intended one that never happened.

**Pass:** exit `0` with `0 finding(s)` and a nonzero path count. Exit `1` lists
findings. Exit `2` means it could not run (an unresolvable ref, or a source
with nothing to compare); that is never a pass.
After changing the script, run `"$guard" --self-test`;
it plants each kind of loss in a scratch repository and passes only when every
one is reported.

## Why commit pairing is not enough

`git range-diff` compares commits, not the resulting tree.

- **After a true merge, every folded commit is an ancestor of the
  consolidation** and pairs with itself, however much the merge resolution
  threw away. A re-land is the same: the original commits are already in the
  base's history.
- **What matters is per path.** A file the source added must exist; a file it
  modified must not still hold its pre-change content; a file it deleted must
  not hold its pre-change content again. That content check is the one that sees
  a resolution reverting a file.
- **Pairing still matters when commits are copied.** After cherry-picks, a
  source commit with no identical patch in the consolidation was dropped or
  rewritten. The script matches by `git patch-id` first, because the `-x` line
  alone makes `range-diff` report every cherry-pick as changed.
- **Byte equality catches whole-file loss, not partial loss.** The script lists
  every path that differs from the source tip as `review:`; read
  `git diff <source-tip> <consolidation> -- <path>` for each one another source
  or the base does not explain.

The specification ratchet covers what the other checks share a blind spot for:
the consolidation must carry at least as many specification files as the base
plus each source's net addition. It is a count, so read the content findings
first; they name the paths.

## Disposition of every finding

Each finding gets exactly one disposition before the merge:

- **Restore it** in a new commit, for example
  `git checkout pr631 -- src/Filters/ByOwner.cs`, then commit. Re-run the guard.
- **Accept it in history** when a rewritten commit differs only by conflict
  resolution (after reading its range-diff), or when two sources added the same
  specification path. Commit the disposition as a trailer, so CI honors it and
  the reviewer sees it; the guard prints each one as `disposition:`.

  ```bash
  git commit --allow-empty -m "Accept the conflict-resolved owner filter restoration" \
      -m "Consolidation-Allow-Rewritten: <sha>"
  ```

  `Consolidation-Allow-Spec-Delta: <n>` works the same way. The
  `--allow-rewritten` and `--allow-spec-delta` options are the local-only
  equivalents.
- **Remove it in its own pull request** when dropping part of a source is the
  point. A consolidation carries its sources whole; the removal, made after it
  merges, is then the reviewed change rather than a side effect of a merge.
- **Stop the consolidation** when the loss cannot be explained.

A merge resolution that reverts a file is a finding even when it was
intentional, because nothing else records the intent. Never clear a finding by
removing its source from the argument list. Explain each disposition in a pull
request comment; the description is the release note and carries no
verification notes.

## Make it a required check

A guard an agent can skip is a suggestion. Run it in CI on every pull request,
discover the sources from history, fail on exit `1` and `2`, and add the job to
the branch's required status checks with no bypass for the identity that
merges. Read [references/ci-required-check.md](references/ci-required-check.md)
when adding or reviewing that workflow.

## Pitfalls

- **Verifying against the revert.** A re-land's source is the original change;
  the revert's own diff only proves the revert was undone.
- **Passing the landed tip as a bare ref.** It is already in the base, so the
  script exits `2` with `already contained`; use `M^1..M^2` or `C^..C`.
- **Trusting a green build.** The specifications that would have failed are the
  ones the merge deleted.
- **Fixing a finding by force-pushing or squashing the branch.** That removes
  the evidence and breaks [git-commits.md](../../rules/git-commits.md); restore
  in a new commit.
- **Folding from a branch name.** A deleted branch cannot be fetched again;
  `refs/pull/<n>/head` can.
