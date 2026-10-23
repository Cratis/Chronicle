<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/required-checks-and-path-filters.md -->
# Required checks, path filters and merge queue

## Contents

- The hang
- Change detection and the aggregator
- Keeping required checks stable
- Merge queue

Use this when skipping work by changed path, when a pull request hangs on a check
that never reports, or when pull requests that pass alone break the default branch
together.

## The hang

A required status check that never reports blocks the merge forever. A workflow
that has a workflow-level `paths:` filter does not start when no path matches, so
its required check stays pending. A job skipped through `if:` or `needs` reports
`skipped`, which counts as passing. So: skip jobs, never whole required
workflows.

## Change detection and the aggregator

Run a cheap `changes` job, gate heavy jobs on its outputs, and require one
always-run aggregator.

```yaml
jobs:
  changes:
    runs-on: ubuntu-latest
    timeout-minutes: 5
    permissions:
      contents: read
    outputs:
      code: ${{ steps.diff.outputs.code }}
    steps:
      - uses: actions/checkout@v7 # pin by full commit SHA
        with:
          fetch-depth: 2
      - id: diff
        run: |
          if git diff --name-only HEAD^1 HEAD | grep -q -v -E '^(Documentation/|.*\.md$)'; then
            echo "code=true" >> "$GITHUB_OUTPUT"
          else
            echo "code=false" >> "$GITHUB_OUTPUT"
          fi

  build:
    needs: [changes]
    if: needs.changes.outputs.code == 'true'
    runs-on: ubuntu-latest
    timeout-minutes: 45
    steps:
      - run: echo build

  checks:
    if: always()
    needs: [changes, build]
    runs-on: ubuntu-latest
    timeout-minutes: 5
    permissions: {}
    steps:
      - name: Require every needed job to pass or be skipped by design
        env:
          RESULTS: ${{ toJSON(needs.*.result) }}
        run: |
          echo "$RESULTS"
          if echo "$RESULTS" | jq -e 'any(. == "failure" or . == "cancelled")' > /dev/null; then
            exit 1
          fi
```

- Require only `checks` in branch protection. Adding a job to the workflow then
  means adding it to `needs`; nothing else changes.
- `if: always()` is essential: without it the aggregator is skipped when a needed
  job fails, and a skipped required check counts as passing.
- List every job in `needs`. `needs.*.result` covers direct needs only, and a
  job missing from the list can fail unnoticed.
- The aggregator itself needs a runner, so it adds one scheduling boundary; keep
  it on a cheap runner with a short timeout.
- Keep workflow-level `paths:` for workflows nothing requires (a docs site
  build). For one event, `paths` and `paths-ignore` cannot be combined; use
  `paths` with `!` negation patterns.

## Keeping required checks stable

Branch protection matches check names. Renaming a job, changing a matrix key or
moving a job to another workflow removes the old check and leaves the rule waiting
on a name that no longer exists. When editing, list the check names before and
after the change and keep them equal, or update the protection rule in the same
step.

Never make a required check conditional on the thing it validates: release,
schema, migration and shared-library checks run on every pull request that can
affect them. If the filter must be wide to be safe, keep it wide.

## Merge queue

Fits when pull requests pass individually and break after merge (semantic
conflicts), and the cost of a broken default branch is higher than the extra
validation. It does not shorten CI; it adds a validation of the queued result.

```yaml
on:
  pull_request:
  merge_group:
    types: [checks_requested]
```

- The required workflows must run on `merge_group`, or the queue waits forever for
  a check that never appears.
- The aggregator must handle both events. For change detection on `merge_group`,
  compare against `github.event.merge_group.base_sha` instead of `HEAD^1`.
- Concurrency groups must keep merge-group runs apart from pull-request runs, and
  must not cancel a merge-group run for a newer pull-request push.
- Build concurrency, merge-group batching and the queue's own settings are
  separate controls; change them one at a time.
