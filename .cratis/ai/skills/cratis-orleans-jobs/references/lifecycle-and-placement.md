<!-- cratis-ai-managed: skills/cratis-orleans-jobs/references/lifecycle-and-placement.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Lifecycle, recovery and placement

## Stop, drain and admission

Requires Cratis.Orleans 1.20.0 for `IJobs.StopAndDrain(key, jobId, timeout)` and the grain protocols `IJobDrain` / `IJobStepDrain`. Unlike ordinary stop acceptance, `Drained` means no step code can run, final outcomes/checkpoints are durable and completion/stop hooks finished. Other outcomes are `TimedOut` (call again), `Completed`, `NotFound` and `UnknownJobType`. `TimeSpan.Zero` polls. Add the method to hand-written `IJobs` implementations. Enable only after every silo supports it. A pending drain survives reactivation and refuses resume; a timeout is not proof of quiescence. Hooks can repeat or be missed after a crash, so keep them idempotent rather than collecting results only there.

Requires 1.21.0 for owner admission: implement `IJobAdmission.MayRun(JobAdmissionRequest, CancellationToken)` and register with `siloBuilder.AddJobAdmission<T>()` on every silo that hosts those job types. Every admission must allow; none means allow. It runs before start, every resume and each later stage. Refusals return `StartJobError.NotAdmitted` / `CannotResumeJobError.NotAdmitted`; manager resume returns false. A refused running job is stopped/drained, and a refused later stage stops the job. Exceptions/timeouts are failures, not refusals; a later stage retries an unanswered admission on a timer.

`MayRun` runs in the job's turn: never call the job, its steps, or a non-interleaving grain that could be waiting on this start/resume. To change what existing work may do, the owner calls `StopAndDrain` **before** changing the admission answer. Older silos do not ask, so admission guarantees require every silo to run the supporting version. Admission does not by itself serialize arbitrary host-owned mutual-exclusion rules.

## Bounded delivery and retries

Requires 1.23.0 for these opt-in execution controls. Defaults preserve unbounded dispatch, one execution attempt and no lease timeout:

| Option | Default / meaning |
| --- | --- |
| `MaxActiveStepsPerJob` | `null` (all); a positive limit fills a dispatch window within a stage |
| `MaxStepAttempts` | `1`, including first attempt; failed work can retry when increased |
| `StepRetryBackoff`, `MaxStepRetryBackoff` | `5 seconds`, `5 minutes`; capped exponential delay |
| `JobWatchdogInterval` | `1 minute`; probes stalled delivery/progress |
| `StepLeaseTimeout` | `TimeSpan.Zero` disables heartbeat lease enforcement |
| `StepDeliveryMaxAttempts` | `10` |
| `StepDeliveryRetryDelay`, `MaxStepDeliveryRetryDelay` | `1 second`, `30 seconds` |

The watchdog restarts a leased step only after its owner silo left the cluster **and** its lease expired. Steps saved by older releases get one lease period. `Job.ShouldRetryStep` decides which failures retry, and `Job.MaxActiveSteps` can override the window. Persist optional `Attempt` and `Lease` on custom step state; old records default to attempt 1 and no lease. `IJobStepStorage.CountByStatus(jobId)` has a default implementation. A step persists its terminal state before retrying outcome delivery; exhausted delivery does not turn that completed step into a new failed execution. The watchdog reconciles stored outcomes if reports never arrive. Keep work idempotent: leases and delivery deduplication do not make external effects exactly-once.

`IJobsManager.RetryFailedSteps(jobId)` is opt-in through `Job.CanRetryFailedSteps` and runs failed steps of a job completed with failures again. Ordinary resume is unchanged. It is not a general rerun of a successful job.

## Step test runs

Requires 1.22.0 for `step.BeginTestRun()` and `JobStepTestRun<TState>.Perform(state)` in a specification host. `Perform` runs `PerformStep` off activation with the supplied state and returns its `Catch<JobStepResult>`; `TryInvokeOnActivation` callbacks run serially on a stand-in scheduler. `IsOnActivation` tests that state changes happen in callbacks. `Stop()` cancels work while admission remains open; `CloseAdmission()` refuses callbacks. A run performs once and records/delivers nothing to a job. Cover `JobStepAlreadyHasALiveRun` and `JobStepTestRunWasSuperseded` misuse. This does not isolate external effects: use safe substitutes, not a live job/store.

## Custom storage and rolling upgrades

Persist the complete `JobState`, including `StopFinalized`, `StoppedStepsCounted` and `CompletedStepsCounted`. Missing lists read as empty for old records; do not project these fields away when storing state. The completion list tracks outcomes in executions begun by resume/retry, not jobs never resumed, and warns at 100,000 entries. Cross-version code that does not persist the list can lose duplicate protection, so keep mixed-version windows short.

A completed stop sets `StopFinalized` after all required notifications have succeeded. Unfinalized stops are retried; finalized stops are excluded from recovery through `IJobStorage.GetJobsExcludingFinalizedStops`. Custom providers can override its default in-memory filtering to filter in storage. MongoDB and SQL filter there; MongoDB adds `status_stopFinalized`. SQLite, PostgreSQL and SQL Server jobs storage are supported; MySQL/MariaDB is not. Removing jobs likewise retries unacknowledged notifications; acceptance of removal is not quiescence or immediate physical deletion.

## Independent controller and worker placement

Requires 1.30.0 for `Cratis.Orleans.Hosting` extensions on `ISiloBuilder`:

- `AddJobPlacement()` or `AddJobPlacement<TDirector>()` opts controllers into `JobsPlacementStrategy`.
- `AddJobStepPlacement()` or `AddJobStepPlacement<TDirector>()` opts workers into `JobStepsPlacementStrategy`, including recovered steps.
- Directors implement Orleans `IPlacementDirector`. Host directors decide eligibility; throw `NoEligibleJobWorker` when no silo is eligible. Orleans does not retry that exception; the job's failure handling applies.
- Without an opt-in, controller placement remains resource-optimized and workers use the cluster default. The built-in directors preserve those defaults when an opt-in has no custom director.
- **Wait until every silo runs the supporting version**, then register each strategy identically on every silo. Do not introduce the strategy during a mixed-version rollout.

`UseJobStepParallelismFromSiloMetadata()` independently sizes each silo's throttle from the positive integer `cratis.jobs.max-parallel-steps` metadata value (or the supplied metadata key). Missing, nonnumeric or nonpositive values fall back to `JobsOptions.MaxParallelSteps` and its processor-count default. Publish with Orleans `UseSiloMetadata`. This is a startup value, not live resizing, authorization or admission. Keep controller placement, worker eligibility, execution throttling and job admission as separate choices.
