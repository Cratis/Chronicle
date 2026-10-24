---
applyTo: ".github/workflows/**"
paths:
  - ".github/workflows/**"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/github-actions.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# GitHub Actions workflows: capacity, cost, and release discipline

> **Why this rule exists.** On 2026-08-25 the organization's hosted runners starved for
> most of a working day: every repository ran its scheduled jobs at the same minute,
> hour-long jobs with no timeout occupied the shared concurrency pool, and a
> comment-triggered assistant booted a runner for every comment in two repositories.
> The remediation that followed is encoded here so it stays true.

## The budget you are spending

- The whole organization shares **one hosted-runner concurrency pool** (20 concurrent
  jobs on the Free plan, 5 for macOS). Every queued job competes with every repository.
- **Public repositories** run free on hosted runners; **private repositories bill every
  minute** — Linux 1×, Windows 2×, macOS 10× — against a small monthly allowance.
- The organization runs its own scale set (`cratis-arc`). Jobs there cost nothing and
  do not touch the hosted pool.

## Scheduling

- **Never schedule on the top of the hour**, and never copy another repository's cron.
  `0 6 * * *` in 36 repositories produced a 270-job stampede into 20 slots. Pick a
  deterministic offset unique to the repository (minute 1–59, spread across 03:00–07:00 UTC).
- A scheduled workflow that exists to keep coverage (nightly full matrix, health probe)
  should run the *narrowest* thing that preserves the claim it exists to make.
- **A scheduled workflow whose subject no longer exists is deleted, not left running.**
  It still queues into the shared pool every day, and a scheduled no-op is the worst
  kind: it never fails, so nothing ever draws attention to it. Deleting it is also the
  only way to stop handing it whatever secrets it was passed — `Cratis/AI` ran a daily
  package update against a repository with no package manifest of any kind, checking out
  the full history under an organization-wide write PAT to find nothing to update.

## Every job, always

- **`timeout-minutes` on every job.** The default is 6 hours; one hung job holds a
  pool slot for all of it. Tiers that work: quick checks 15, builds 30–45,
  publish/release 60, integration/benchmarks 120.
- **A `concurrency` group on every pull-request verification workflow** with
  `cancel-in-progress: true`, keyed `${{ github.workflow }}-${{ github.ref }}`.
  **Never** cancel-in-progress on publish, release, or deploy workflows — a cancelled
  half-publish is worse than a queued one.
- **Publish and release workflows never cancel** (`cancel-in-progress: false`) and set
  `queue: max`. A group holds one running and one pending run; a newer arrival replaces
  the pending one. Merges into the default branch share a group (a push, a repository-wide
  key, or `github.ref` on a merged `pull_request` `closed` event, which is the base
  branch), so without `queue: max` a newer merge replaces the older pending release.
  `queue: max` keeps up to 100 pending runs, processed in order, and applies only while
  `cancel-in-progress` is false or unset. Keep a single pending slot only for a documented
  design where each run releases everything since the last release, so a replaced run
  loses nothing; say so in a comment beside the `concurrency` block.
- **A required check must not be skippable by `paths:` alone.** A workflow that never
  starts never reports, and the pull request hangs. Gate jobs on a change-detection job
  and require one always-run aggregator job (`if: always()`, checks every `needs.*.result`).
- **Pin third-party actions by full commit SHA** (version in a comment) and set minimal
  `permissions:` on each job. Run `zizmor` locally on edited workflows: its `unpinned-uses`
  and `excessive-permissions` audits check exactly this. `actionlint` checks syntax,
  expressions and embedded shell, not pinning. Do not assume CI runs either tool.
- To make a workflow faster, use the `cratis-ci-pipeline-performance` skill.

## Matrices and operating systems

- Run the full OS matrix on `schedule`/`workflow_dispatch`; run **Linux only on pull
  request syncs**. Windows bills double and macOS ten-fold, and per-PR duplication has
  to earn that cost with unique signal. (Measured before adopting this: 75 dual-OS runs
  of one private repository, zero Windows-only failures.)
- Skip the expensive job entirely for documentation-only changes: a cheap change-detection
  job (`git diff --name-only HEAD^1 HEAD` on the merge commit) gating the build job.

## Runners

- Private-repository jobs belong on the organization scale set: `runs-on: cratis-arc`,
  or a repository variable such as `${{ vars.<REPO>_RUNNER || 'ubuntu-latest' }}` so
  routing changes without a code change.
- On self-hosted runners, `actions/setup-dotnet` cannot write `/usr/share/dotnet`.
  Set `DOTNET_INSTALL_DIR: ${{ runner.temp }}/dotnet` on the setup step.
- **Never** route `pull_request`-triggered jobs of a *public* repository to self-hosted
  runners — that hands code execution on our infrastructure to any fork.

## Automation that writes back

- A bot that pushes to a branch must **retry with `git pull --rebase`** — the branch
  moves while the job runs, and a plain push loses the whole run to a non-fast-forward.
- A bot's pre-push verification build must match the strictest configuration any CI in
  the organization applies to the same commit (build `Release` when consumers enforce
  analyzers there). Do not add `[skip ci]` to bot commits as a load optimization: the
  downstream CI run is the only check that sees the merged result. One exception: a
  bootstrap commit that changes **only** bootstrapped wrapper workflow files, whose
  content the source repository's CI verified before the rollout, may carry
  `[skip ci]` once the same wrapper content has landed with CI running in one canary
  repository. Package-update bots, and any commit touching another file, keep normal CI.
- Comment-triggered agent workflows must gate on the trigger phrase in the job's `if:`
  (for example `contains(github.event.comment.body, '@claude')`) so a runner only
  starts when the agent is actually addressed.

## Releases

- A pull request that changes nothing outward-facing — workflow edits, CI configuration,
  documentation — carries the **`no-release`** label, never `patch`. See
  [`pull-requests.md`](./pull-requests.instructions.md). Merging config-only work under `patch` cut
  four unintended releases on 2026-08-25.
- Every repository's release-intent gate must accept `no-release`; a gate that only
  accepts `major`/`minor`/`patch` forces exactly that mistake.
