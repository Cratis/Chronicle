---
name: cratis-ci-pipeline-performance
description: Make a Cratis repository's GitHub Actions CI/CD faster and cheaper by measuring workflow timings first, picking the lever that fits the measured bottleneck (build once and shard, caching, path filters with an aggregator, concurrency, runner routing, publish from artifacts) and verifying before and after. Use when a workflow is slow, queues, burns runner minutes, reruns or cancels a lot, or when editing .github/workflows to speed up CI. Not for authoring a release (use cratis-governed-release-methodology), recurring regressions on one surface (use cratis-engineering-regression-cluster), runtime performance of application code (use cratis-performance-review), or fixing a red build.
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/SKILL.md -->

# Measure the workflow, then pick the lever

A workflow is slow for one of four different reasons, and each has a different
fix. Adding runners cures a queue and does nothing for a serial build; a faster
build does nothing for a workflow that fails a quarter of the time. Measure,
classify, change one lever, measure again. Never optimize from the YAML alone.

Capacity, timeout, concurrency and runner invariants live in
[github-actions.md](../../rules/github-actions.md); obey them and do not restate them in a change.
Workflow-only pull requests carry the `no-release` label per
[pull-requests.md](../../rules/pull-requests.md).

## 1. Measure first

Execute the bundled script (needs `gh` authenticated for the repository, and `jq`):

```bash
bash scripts/ci-timings.sh --repo OWNER/REPO --workflow ci.yml --limit 50 --jobs 10
```

Add `--branch main` or `--event pull_request` to compare populations; PR and
default-branch runs behave differently. `bash scripts/ci-timings.sh --self-test`
proves the script itself works without network access. Exit codes: `0` report
printed, `2` usage error, `3` no runs or no data, `4` missing `gh`/`jq` or `gh`
not authenticated. Exit `3` and `4` are an unknown, not a clean workflow: say the
measurement was not made.

The report separates **wall clock** (what a developer waits for) from
**runner-minutes** (what the organization pays for or occupies). A change can
cut one and raise the other; report both. It prints a one-line classification:

| Class | Hint threshold | Meaning | First lever |
|---|---|---|---|
| failure-bound | failed or rerun >= 15% of completed runs | Time and minutes go into runs that are redone | Flaky and hung jobs; fail fast |
| cancel-bound | cancelled >= 20% | Superseded work still ran heavy legs | Cheap gate first, path filters, smaller PR matrix |
| queue-bound | median longest job wait >= 25% of wall | Jobs wait for runners, not for work | Fewer, bigger jobs; runner routing |
| execution-bound | none of the above | The critical path is serial work | Build once and shard; caching |

The thresholds are a hint, not a verdict. Several classes can hold at once; the
script lists extras after the primary class. Then find the **critical path over
the required checks**: the longest chain of `needs` from the start to the last
required job. Only work on that chain shortens wall clock; shaving a parallel
sibling does not.

Without `gh` access, read the workflow YAML, state that the result is
**static-only analysis**, and rank findings as hypotheses to measure later.

## 2. Guardrails before proposing a fix

- **Never hide required validation.** Release, schema, migration, analyzer and
  shared-library checks stay on the path that gates a merge or a release. Moving
  them to a schedule needs an explicit owner decision, not an optimization.
- **Say what less parallelism costs.** Merging jobs lowers queue and setup cost
  but lengthens the critical path when the merged work was parallel. State the
  expected wall change and runner-minute change together.
- **Separate repository changes from organization settings.** Workflow YAML is a
  repository change. Runner capacity, scale sets and plan limits are not; report
  them as a request to the owner and do not work around them in YAML.
- **One lever per change.** Two levers at once make the measured difference
  unattributable.
- **Rank by what it saves.** Estimate minutes per day as (saving per run) x
  (runs per day) from the report, and take the top one to three items.

## 3. Lever catalogue

Each lever lists when it fits, the Cratis default and the main pitfall. Read the
named reference before applying one.

**Build once, shard the tests.** Fits execution-bound workflows where build and
tests run serially or several jobs each rebuild. Default: one job builds in
`Release`, uploads the output with `actions/upload-artifact`, and a matrix of shard jobs downloads it and runs
`dotnet test --no-build`. Pitfall: a shard whose filter matches nothing still
passes, so assert that every shard ran tests. Read
`references/build-once-and-shard.md` when applying or reviewing this.

**Fewer, bigger jobs.** Fits queue-bound workflows: every job pays a scheduler
start, checkout and toolchain setup. Default: merge small sibling jobs that share
a setup; keep shards only where the work is long enough to repay a start. Pitfall:
merging the long pole into a serial chain. Read `references/build-once-and-shard.md`.

**Dependency caching.** Fits any job with a measurable restore or install step.
Default: the shared `Cratis/Workflows` composite actions `dotnet-cache` and
`yarn-cache` (restore on pull requests, save on the default branch), NuGet lock files and
`yarn install --immutable`. Pitfall: a cache key that changes every run, or a
different path list per workflow, which creates separate caches. Read
`references/caching.md`.

**Docker layer cache.** Fits image builds that repeat unchanged layers. Default:
`docker/build-push-action` with `cache-from`/`cache-to` of type `gha` and a custom
`scope` per image and platform. Pitfall: the default scope is shared, so builds
overwrite each other's cache. Read `references/caching.md`.

**Path filters with an always-run aggregator.** Fits cancel-bound workflows and
documentation-only changes. Default: filter heavy jobs by changed paths, and make
a single aggregator job with `if: always()` check every `needs.*.result`; require
only the aggregator. Pitfall: a required check skipped by `paths:` never reports
and the pull request hangs. Read `references/required-checks-and-path-filters.md`.

**Concurrency.** Pull-request verification uses `cancel-in-progress: true` keyed
by workflow and ref. Publish never cancels; where releases share one group, `queue:
max` keeps up to 100 pending runs in order instead of replacing the single pending
one. Pitfall: leaving the single pending slot where a replaced merge's run alone
would have released something (merges into the default branch share a group, also
when keyed on `github.ref` for merged `pull_request` `closed` events). A workflow
whose release tooling aggregates since the last release may keep one pending slot,
documented. Read `references/publish-pipelines.md`.

**Runner routing.** Fits queue-bound workflows. Default: private-repository jobs
on the organization scale set `cratis-arc` or a repository variable that falls
back to a hosted label, hosted runners for public repositories. Pitfall: never
route `pull_request` jobs of a public repository to self-hosted runners; that is
fork code on our infrastructure. The rule owns the details.

**Publish from tested artifacts.** Fits release workflows that rebuild what the
pull-request run already built and tested. Default: build and pack once, then
publish exactly those packages. Pitfall: a second build can differ from the
tested one. Read `references/publish-pipelines.md`.

**Fail fast on cheap gates.** Fits failure-bound and cancel-bound workflows.
Default: formatting, lint and unit-level checks run first, and expensive legs
depend on them with `needs`. Pitfall: serializing checks that were parallel; keep
the cheap gate short enough to repay its place on the critical path.

**Expensive matrices off the PR path.** Fits cancel-bound workflows with a wide
OS or framework matrix. Default: full matrix on `schedule` and
`workflow_dispatch`, a Linux-only representative smoke on pull requests (the
rule states this). Pitfall: the full matrix needs an owner who reads its failures.

**Flaky and hung jobs.** Fits failure-bound workflows. Default: a
`timeout-minutes` on every job, `--blame-hang`, retry only a diagnosed flake, and
quarantine a test with an issue. Pitfall: a blanket rerun hides the defect and
doubles minutes. Read `references/flaky-and-hung-jobs.md`.

**Merge queue.** Fits repositories where passing pull requests break each other
after merge. Default: add the `merge_group` trigger, and make the aggregator
handle it. Pitfall: without `merge_group` the required check never reports in the
queue. Read `references/required-checks-and-path-filters.md`.

**Compile once, analyze once.** Fits any workflow where one push compiles the same
code several times. Default: build once and reuse the output; run analyzers in one
parallel lint job instead of in every build, and let that job fail early on compile
and analyzer errors before long test legs. Pitfall: dropping analyzers from the only
build that consumers enforce. Read `references/test-setup-cost.md`.

**Cores per job before job count.** Fits uniformly slow jobs. Default: check `nproc`
and the runner's resources before sharding; undersized runners make every job slow.
Pitfall: adding jobs to an undersized pool. Read `references/runner-sizing-and-images.md`.

**Hidden test setup.** Fits test lanes whose step time far exceeds the runner's
reported test time. Default: profile the test host and fix per-class setup upstream
rather than sharding it. Pitfall: sharding that divides the waste. Read
`references/test-setup-cost.md`.

**Test a guarantee where it can break.** Fits suites that re-test a dependency.
Default: run them upstream, or on a dependency pin change or nightly; use snapshots
for shape and post-deploy synthetic checks where possible. Pitfall: relocating the
only check of a required guarantee. Read `references/test-setup-cost.md`.

**Images and runner contents.** Fits jobs that pull unused images or install tools per
run. Default: pull only what the job uses, in parallel, and bake tools into the runner
image; content-addressed caches before warm volumes, publish stays cold. Read
`references/runner-sizing-and-images.md`.

Speed work must not weaken supply-chain hygiene. Read
`references/security-and-pinning.md` when a change adds or updates an action,
touches `permissions:`, uses `pull_request_target`, or adds a cache.

## 4. Verify

1. Re-run the same `ci-timings.sh` command on the same workflow after enough new
   runs exist (at least the sample size of the baseline; say so when fewer).
2. Report expected against measured savings, wall and runner-minutes separately,
   and whether the classification moved. A saving that is not measured is labeled
   as expected.
3. Run the linters locally on every edited workflow when installed: `actionlint`
   (syntax, expressions, embedded shell) and `zizmor` (security audits such as
   `unpinned-uses` and `excessive-permissions`). Pass means exit `0` with no
   findings; a tool that is not installed is reported as not run, not as clean.
   Do not assume CI runs either.
4. Compare required check names before and after; a renamed job silently removes
   a required check. See `references/required-checks-and-path-filters.md`.
5. Confirm the workflow change ships as `no-release`.

## References

- Read `references/build-once-and-shard.md` when changing how a build is shared across jobs or how tests are split.
- Read `references/caching.md` when adding or changing a dependency, Docker or build cache.
- Read `references/required-checks-and-path-filters.md` when adding path filters, an aggregator, or `merge_group`.
- Read `references/publish-pipelines.md` when a release or deploy workflow queues, drops runs or rebuilds.
- Read `references/flaky-and-hung-jobs.md` when runs fail, hang or are rerun.
- Read `references/test-setup-cost.md` when test steps outlast reported test time, tests duplicate a dependency's suite, or compiles repeat per push.
- Read `references/runner-sizing-and-images.md` when jobs are uniformly slow, pull images, install tools or reuse workspaces.
- Read `references/security-and-pinning.md` when a change touches actions, permissions, triggers or caches.
- Read `references/sources.md` when attributing or extending the method.
- Execute `scripts/ci-timings.sh` for every measurement; read it only to change it. Its self-test reads the synthetic inputs `scripts/fixtures/runs.json` and `scripts/fixtures/jobs/1003.json`, `scripts/fixtures/jobs/1007.json`, `scripts/fixtures/jobs/1010.json`; edit them only together with the expected lines in the script.
