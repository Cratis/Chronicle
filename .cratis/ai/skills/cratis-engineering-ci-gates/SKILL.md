---
name: cratis-engineering-ci-gates
description: Use when choosing CI-matched local gates, waiting for exact workflow runs or considering cancellation of owned superseded checks. Not for delivery grouping, review rounds or optimizing workflow design.
license: LICENSE
compatibility: Requires git and the GitHub CLI (gh).
---
<!-- cratis-ai-managed: skills/cratis-engineering-ci-gates/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Run the gate once, then watch the exact run

Operate checks; do not redesign workflows here. Read
[verification-discipline.md](../../rules/verification-discipline.md) for phase
budgets and exit-status handling, and
[pull-requests.md](../cratis-commits-and-pull-requests/references/pr-labels-and-release.md#quality-gates) for required checks.
Read [capability-is-not-authority.md](../../rules/capability-is-not-authority.md)
before an external effect. A request to verify does not authorize a push.

## Derive the local gate

1. Read `.github/workflows/` and the repository's tier policy. Follow reusable
   workflows and invoked scripts. Extract commands, solution and test selection,
   configuration, warning settings, analyzers, lint and formatting checks.
2. Require an executable, repository-specific specification-project coverage
   gate in CI; add one if absent and run its documented command locally where
   inputs are available. Discover every spec project independently of CI's test
   selection, then compare discovery with actual CI test commands and results.
   Every project must run in some CI test job or be removed; a green subset,
   a build-only job or solution membership is not execution coverage.
   Follow reusable workflows and scripts, expand matrices and evaluate job/step
   conditions for the relevant event and head SHA. Resolve project selection and
   test filters; a command whose filters exclude all of a project's specs does
   not cover it. A skipped conditional job cannot cover its assigned projects.
   Keep command/filter mapping separate from runtime proof: correlate per-project
   test results with the executed jobs for that SHA and require nonzero executed
   specs per project. A static mapping alone cannot prove the tests ran.
   Fail closed on empty or unknown discovery, unresolved selection/conditions,
   missing results or uncovered projects. Print omitted project paths and the
   missing/excluding job, command or filter so omissions are actionable. Pass only
   when every discovered project has execution coverage; use repository tooling,
   not a universal framework-specific scanner in this corpus.
3. Prefer the repository's existing gate script. If none exists, keep a small
   local runner under `.ai-work/`, with each phase's real exit status preserved.
   Do not substitute only the touched projects for the final gate. The done gate
   runs its tier's CI shards unfiltered; filters are for iterating only.
4. **Tier 1** is the whole-solution build with CI's configuration and warning
   settings, lint/format, and every non-integration test project. For example,
   use Release with warnings as errors when that is what CI uses. For a
   dependency or analyzer bump, that exact CI Release build is the gate; a Debug
   build is not evidence.
5. **Tier 2** adds integration tests. The repository's tier policy decides
   whether they run locally. The default is CI-backed integration only if CI
   runs that coverage, it passes before merge, and reviewer notes say it was
   deferred to CI. If CI lacks that coverage, do not claim it is verified.
6. Name anything CI runs that cannot run locally: services, platforms, secrets
   or hosted checks. Say what ran instead and what remains unverified before
   pushing. Missing coverage is not a local pass.
7. A change to a command, read model or query shape runs the repository's proxy
   and API regeneration checks; never hand-edit generated files. A package bump
   regenerates the checked-in snapshots that pin versions, in the same commit.
   Arc TypeScript proxies are generated and committed by the client, never in CI:
   CI, and local gates that mirror CI, pass `-p:CratisProxiesSkip=true -p:CratisProxiesOutputPath=`
   (Skip needs Arc 22.185.1 or later; the empty path covers earlier Arc); only the
   client's own Debug hook regenerates proxies. See the
   **cratis-arc-command** proxy-generation reference.

Use affected-project checks while coding and writing specifications. Build once
at the end: run the full local gate when the work is believed complete, not after
every edit. Do not repeat a passing gate on unchanged inputs for reassurance.
When several deliveries form one candidate, the assembler runs the final gate
on the assembled state; deliveries bring targeted results. A changed state after
a failure needs the required gate again.

Keep build, lint and test phases separate, each with a deadline. Stop on failure;
do not run dependent phases or hide a producer's exit status behind a filter.
Report the exact command and result. Put verification and deferred coverage in
reviewer comments, not the release-note description.

## Local first, and resource-aware

CI is the final confirmation before merge and deploy, never a feedback loop.
Run build, specifications, lint and frontend gates locally until green, then
push. Do not push to see whether something compiles or passes, and do not use
a red run to discover the next failure.

Several agents, worktrees or builds often share one machine. Keep it responsive:

- Cap parallelism, for example `dotnet build -m:2`, built spec executables with their
  runner-specific thread limit, and `vitest run --maxWorkers=2`. Do not pass MSBuild
  `-m` to executables; `dotnet test <project> --no-build -m:2` is VSTest-only.
- Run one heavy phase at a time (build, then specs, then frontend lint and
  tests), never several at once.
- Do not start a heavy phase while another agent's runs in the same machine's
  checkout; wait or lower the cap, and do not retry a starved or timed-out phase
  at full parallelism.

## Wait without polling

After an authorized push, capture the full head SHA and discover the run for the
intended workflow. For example:

```bash
head_sha=$(git rev-parse HEAD)
gh run list --repo OWNER/REPO --workflow ci.yml --commit "$head_sha" \
  --json databaseId,headSha,event,status
```

Select the exact run ID, not the newest run by time or branch alone. Disambiguate
multiple events or dispatches for the same SHA. Capture one ID per required
workflow. If no matching run is visible, report that no run was captured; do not
start a status-query loop or guess an ID.

Start one blocking watch for that run, with a deadline sized to the repository:

```bash
gh run watch <id> --repo OWNER/REPO --exit-status
```

A background job, if the harness has one, can hold the same blocking watch.
Never re-query status by hand. Use the watch's completion and exit status, not
repeated `gh run list`, `gh run view` or PR-check requests while waiting.

Say: "Waiting on CI run <id> for the current PR head." Do independent work while
it runs. If none remains, end the turn. If the watch cannot finish in the session,
leave a handoff under `.ai-work/` with the SHA, run ID and outstanding checks;
report the wait as incomplete, not successful. A watch timeout does not cancel
the remote run and is not a failed-test diagnosis.

## Diagnose once, fix together

After the run completes with a failure:

1. Read the failed logs once, for example
   `gh run view <id> --repo OWNER/REPO --log-failed`.
2. Check other jobs in that completed run, for example
   `gh run view <id> --repo OWNER/REPO --json jobs`. Account for every failure,
   including independent failures beyond the first red job.
3. Fix all in-scope causes together. Report unrelated or unavailable causes as
   blockers rather than silently waiving them. Re-run the required local gate
   on the fixed state, then push once when the request authorizes it.
4. Flaky tests never hang a release. Classify within about 15 minutes from one
   failed-log read and the test's history on main: real (in code the change
   touched) or flaky. A flaky or pre-existing claim needs independent root-cause
   validation; an unknown or repeating failure without a matching signature is
   real and a blocker, not a quarantine candidate.
   - Confirmed flake: after the run completes, rerun once, e.g.
     `gh run rerun <id> --repo OWNER/REPO --failed`; a known flake with a
     matching signature may go straight to quarantine. If it fails again,
     quarantine the test in the same train via the repo's mechanism, open a
     tracked "Flaky: <test>" issue with the evidence (label `flaky`), and push
     once. Never quarantine silently or disable security or critical-path
     coverage; if the security lens vetoes, evict the item or escalate.
   - Real failure: fix in one round or evict the item. Never retry in loops; a
     train waits at most one CI cycle on a flake.

Never put a CI skip token in a title or commit. Do not spend a push on each
individual fix or use CI to discover whether speculative fixes compile.

## Cancel only owned, superseded work

Cancellation needs a reason and an exact target. Within an authorized shipping
or folding request, cancel only runs started by the session's own superseded or
folded pushes. Anyone else's run needs the owner's agreement. Merely sharing a
branch or finding a duplicate run does not establish ownership.

Before acting, inspect each candidate, for example:

```bash
gh run view <id> --repo OWNER/REPO \
  --json workflowName,event,headBranch,headSha,status,createdAt
```

Confirm the initiating push, workflow and SHA against the superseding head or
folded PR. Inspect the workflow's jobs if its effects are unclear. Never cancel
a release, publish or deploy run, regardless of ownership or workflow name.
Do not cancel a check whose result a queued or running publication needs, or a
required check about to gate a merge. Another owner's watched merge run needs
that owner's agreement. Even with agreement, release, publish and deploy runs
and checks needed by publication remain protected.

Re-read immediately before cancelling so drift cannot widen the target. Do not
cancel by branch, name or age alone, or use a bulk cancellation command. Once
confirmed, for example:

```bash
gh run cancel <id> --repo OWNER/REPO
```

Report one line naming the run ID and superseding or folding reason. If ownership
or downstream use is uncertain, leave it running and report the uncertainty.

## Integration and containers

Run local integration only when the tier policy calls for it or a bounded
cross-repository proof needs it. Give every container phase and Docker call a
deadline, including startup, image builds, tests and teardown. A background job
is not a timeout or a cleanup policy.

Keep track of the containers and networks created by the phase. On success,
failure, timeout or abandonment, stop and remove only those owned resources.
Use exact IDs or a task-specific Compose project; confirm ownership before
removal. Do not restart a shared daemon or stop another task's services.
Never run `docker system prune -a` or `docker image prune -a`.

When a test run hangs, ask for a mini hang dump (`--blame-hang-dump-type mini` on VSTest, `--hangdump-type Mini` on Microsoft.Testing.Platform). Read the stack (for example `dotnet-dump analyze <file>` then `clrstack -all`), keep the text output, and delete the `.dmp` file. Use a full dump only for a named investigation that needs heap contents.

For a repository's Compose test setup, one scoped teardown example is
`docker compose -p <task-project> down`, under its own deadline. Do not add
volume deletion unless those volumes are disposable and owned by this task.
Keep secrets out of logs and artifacts per
[secrets.md](../../rules/secrets.md).

## What breaks

- An affected-project build presented as Tier 1 leaves other projects unchecked.
- A green Tier 1 presented as integration proof hides coverage deferred to CI.
- Watching a branch's newest run can report success for the wrong SHA.
- Manual polling duplicates the watch and wastes time without improving proof.
- Blind retries and one push per failure spend checks without fixing the run.
- Broad container cleanup destroys resources the phase does not own.

## How it is proven

The derived local commands exit successfully on the final state. The blocking
watch for each required run exits successfully for the exact head SHA, including
integration coverage deferred to CI. A timeout, cancellation, missing check or
unobserved result stays unverified. Report local results, CI results and omissions
separately; no inventory or receipt substitutes for running the checks.
