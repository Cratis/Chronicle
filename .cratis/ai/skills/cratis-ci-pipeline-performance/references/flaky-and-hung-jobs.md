<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/flaky-and-hung-jobs.md -->
# Flaky and hung jobs

Use this when the report is `failure-bound` (failed or rerun at or above the
hint threshold) or a job occasionally runs to its timeout. Reruns and failures
cost runner-minutes and developer trust; diagnose before retrying.

## Diagnose

1. Run `ci-timings.sh` with `--branch main` and again with `--event pull_request`.
   Failures on the default branch with no code change are flakes or environment
   problems; failures only on pull requests are mostly real.
2. Group the failing runs by job and step name. One step that fails across many
   runs is a defect in that step, not noise.
3. Separate the cause: a hung test, a timeout from an external service, an
   out-of-memory kill, or an infrastructure cancellation.

## Bound every job

- `timeout-minutes` on every job, per the rule. A hung job otherwise holds a
  runner slot for up to six hours.
- Set the job timeout above the test timeout, so dump collection and cleanup have
  time to finish.

## Find hangs and crashes in .NET

```bash
dotnet test Repo.slnx --configuration Release --no-build \
  --blame-hang --blame-hang-timeout 5m --blame-crash \
  --results-directory TestResults
```

- This is the VSTest form. Microsoft.Testing.Platform projects use their own
  crash and hang dump extensions and options; do not mix the two sets of switches.
- Upload `TestResults` as an artifact with `if: ${{ !cancelled() }}`, so the dump
  survives the failure.

## Retry only a diagnosed flake

- Retry the smallest unit: a single test or a single failed job, never the whole
  suite. With Microsoft.Testing.Platform, the retry extension provides
  `--retry-failed-tests` with a failure-percentage ceiling; plain VSTest has no
  equivalent flag.
- Keep the first-attempt failure visible. A retry that turns red green without a
  record hides the flake and doubles minutes.
- Retrying a deterministic failure only consumes capacity.

## Quarantine

A quarantined test is a decision with an owner, not a deletion:

- Mark it with the framework's skip or category mechanism and a reason that names
  an issue.
- Run quarantined tests in a separate, visible, non-required lane so they keep
  producing signal.
- Give the issue an owner and an expiry; an entry nobody revisits is a check
  quietly deleted.

## Fail fast

Put the cheap, high-signal gates (format, lint, unit-level tests) before the
expensive legs and make the expensive legs `needs` them. A failure then costs
minutes instead of the full pipeline. Keep the gate short: a slow gate on the
critical path moves the cost instead of removing it.

## Cancelled work

A high cancelled share is expected on pull requests with `cancel-in-progress`.
It is waste only when the cancelled runs had already spent their expensive legs.
Reduce it by ordering cheap legs first and by keeping the full matrix off the
pull-request path, not by removing the cancellation.
