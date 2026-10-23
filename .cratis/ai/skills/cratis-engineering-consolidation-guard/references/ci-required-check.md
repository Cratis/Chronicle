<!-- cratis-ai-managed: skills/cratis-engineering-consolidation-guard/references/ci-required-check.md -->
# Consolidation guard as a required CI check

## Contents

- What makes it unskippable
- Workflow
- Make the check required
- What discovery cannot see

## What makes it unskippable

An agent asked to "run the guard before merging" can forget, run it against the
wrong sources, or read exit `2` as a pass. A required check removes all three:

- **It runs on every pull request.** The job decides whether the branch is a
  consolidation, so no one has to label it. A workflow-level `paths` filter
  would leave the required check pending on unrelated pull requests; decide
  inside the job instead.
- **It discovers the sources from history.** `--discover` reads merge parents
  the base does not contain, `cherry-pick -x` lines and reverts of reverts. The
  author does not have to declare anything, and cannot leave a source out.
- **It fails on `1` and on `2`.** A source that cannot be resolved or compared
  fails the check; unknown is not pass. A `cherry-pick -x` of a commit that was
  never pushed, or exists only on a fork, stays at `2`: push the source commit
  so the job can fetch it. No trailer clears an unresolvable source.
- **Dispositions live in history.** An accepted rewrite or specification delta
  is a `Consolidation-Allow-Rewritten:` or `Consolidation-Allow-Spec-Delta:`
  commit trailer, which the job honors and prints as `disposition:`. Accepting a
  loss is therefore a reviewable commit, never a bypass. The job reads trailers
  from every commit the consolidation adds, folded source commits included, so
  a reviewer reads each `disposition:` line, not only the consolidation's own
  commits.
- **Merging requires it.** A red or missing required check blocks
  `gh pr merge`. With no bypass actor, `gh pr merge --admin` cannot get past it
  either.

## Workflow

Copy `scripts/verify-consolidation.sh` from this skill into the repository's
own `scripts/` folder and commit it. A CI gate must not change underneath the
repository when the shared corpus is updated, and a copy the repository owns
changes only through its own pull requests. Run the copy's `--self-test` in the
same job so a broken edit to it cannot pass silently.

```yaml
name: Consolidation guard

on:
  pull_request:
    types: [opened, synchronize, reopened]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  consolidation-guard:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ github.event.pull_request.head.sha }}
          fetch-depth: 0

      - name: Fetch pull request heads
        # Cherry-pick and revert sources can live only on deleted branches; their pull request refs remain.
        run: git fetch --no-tags origin '+refs/pull/*/head:refs/remotes/origin/pull/*'

      - name: Self-test the guard
        run: scripts/verify-consolidation.sh --self-test

      - name: Verify every folded and re-landed source survives
        env:
          BASE: origin/${{ github.base_ref }}
        run: |
          set -euo pipefail
          sources="$(scripts/verify-consolidation.sh --discover --base "$BASE" HEAD)"
          if [ -z "$sources" ]; then
            echo "No folded, cherry-picked or re-landed sources between $BASE and HEAD; checked $(git rev-list --count "$BASE..HEAD") commits."
            exit 0
          fi
          printf 'Sources:\n%s\n' "$sources"
          # Word splitting is intended: one source per line.
          # shellcheck disable=SC2086
          scripts/verify-consolidation.sh --base "$BASE" HEAD $sources
```

Notes on the choices:

- `ref: ${{ github.event.pull_request.head.sha }}` checks out the branch as
  pushed. The default pull request checkout is GitHub's synthetic merge with the
  base, whose own merge commit would be discovered as a fold.
- `fetch-depth: 0` brings the full history and the base branch as
  `origin/<base>`; discovery and merge-base need both.
- Fetching every pull request head costs time in a large repository. Drop that
  step only if the repository never restores from deleted branches; an
  unresolvable source then fails the check with exit `2`, which is the safe
  direction.
- Keep this a `pull_request` job on a hosted runner in a public repository; it
  needs only read access.
- Repositories with specifications outside `for_*`/`when_*` folders pass
  `--spec-pattern` to the verifying invocation.

## Make the check required

Add the job's check name, `consolidation-guard`, to the default branch's
required status checks: a repository ruleset with **Require status checks to
pass**, or classic branch protection with **Require status checks to pass
before merging** and **Do not allow bypassing the above settings**. Leave the
ruleset's bypass list empty, or at least without the identity agents merge as.

Renaming the job changes the check name. The old required check then waits for
a status that never arrives, which blocks every pull request; update the
required check in the same change.

## What discovery cannot see

Discovery reads only what history records. Work restored by copying files,
re-implemented by hand, or folded with a squash leaves no trace, which is one
more reason [git-commits.md](../../../rules/git-commits.md) forbids squashing.
For such a branch, run the guard locally with explicit sources and post the
command and its result as a pull request comment, so the reviewer can repeat it.

A file a source added and the consolidation deliberately removes has no
trailer: the check stays red until the file is restored. Carry the source whole
and remove the file in its own pull request after the consolidation merges, so
the removal is reviewed as a change of its own.
