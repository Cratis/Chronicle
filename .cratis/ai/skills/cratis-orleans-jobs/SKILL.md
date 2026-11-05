---
name: cratis-orleans-jobs
description: Use when authoring or recovering Cratis.Orleans jobs, configuring their storage or changing job-step lifecycle transitions. Not for operating live Chronicle jobs or validating application commands.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-orleans-jobs/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Recoverable Orleans jobs

For services and kernel contributors using Orleans. This is library-consumer guidance, not the application vertical-slice architecture. For live Chronicle operations use **cratis-chronicle-cli-operations** instead. Co-hosting conventions are in [orleans.md](../../rules/orleans.md).

## 1. Choose a job, not a detached task

A job plans recoverable work; each step is a grain and does the work. Use it for work that must outlive a request. It is **at-least-once**, not exactly-once: a step interrupted before a durable checkpoint can perform the same work again. Protect external effects with receiver-owned idempotency/deduplication; a checkpoint does not atomically commit an external write.

- Job: derive `Job<TRequest, TJobState>`; the request is a class implementing `IJobRequest`, the state derives `JobState`. Implement `PrepareSteps(TRequest)` returning `Task<IImmutableList<JobStepDetails>>`.
- Step: derive `JobStep<TRequest, TResult, TState>`; state derives `JobStepState`. Its constructor receives `[PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<TState>`, `IJobStepThrottle`, and `ILogger<JobStep<TRequest, TResult, TState>>`.
- Implement `PrepareStep(TRequest)` → `Task<Result<PrepareJobStepError>>`; `InitializeState(TRequest)` → `ValueTask`, called after preparation succeeds; and `PerformStep(TState, CancellationToken)` → `Task<Catch<JobStepResult>>`. `TResult` is the step's result type; the work method returns the framework's `JobStepResult` envelope, not `TResult` directly.
- Give the concrete job and step unique `[GrainType("product-specific-name")]` names. Use serializable request/state/result types. Do not reuse a class-derived grain name across assemblies.

`PerformStep` runs **outside the activation context** with a deep-copied state snapshot. Mutating that snapshot does not persist it; do not access/write grain `State` directly from background work. A specification host such as Orleans.TestKit must register `DeepCopier<TState>`.

Requires Cratis.Orleans 1.19.0 for `TryInvokeOnActivation` to coordinate state/checkpoints on the current activation, including during deactivation. Call it only from the run's background work. Keep callbacks local: capture copied inputs, call remote systems outside the callback, then commit in a second callback. Never call another grain, nest and await another callback, or start unawaited work inside it. Inspect `ActivationCallbackRejected` (`NoRun`, `StaleRun`, `Closed`, `Cancelled`); exceptions propagate. The optional `queueCancellationToken` cancels waiting callbacks only; started callbacks finish. Stop/deactivation waits for started callbacks and work to settle; `JobsOptions.StepStopTimeout` defaults to 10 seconds and `JobStepError.StopTimedOut` is not proof of quiescence. Requires 1.16.2 for framework-owned stop reporting: do not report cancellation again from a `PerformWork` override.

## 2. Plan dependencies as stages

Default steps all use `JobStepStage.First`. Prefer independent fan-out. Use stages only when correctness requires earlier work to finish.

Planning excerpt inside an existing `Job<RecoveryRequest, RecoveryState>`. It assumes `RecoveryRequest : IJobRequest`, `RecoveryState : JobState`, and the two existing grain interfaces `IRepairIndex` and `IRebuildCatalog`, each deriving a typed `IJobStep<,,>`:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RecoveryRequest request) =>
    Task.FromResult<IImmutableList<JobStepDetails>>(
    [
        CreateStep<IRepairIndex>(request, stage: 0),
        CreateStep<IRebuildCatalog>(request, stage: 10)
    ]);
```

Import `System.Collections.Immutable` and `Cratis.Orleans.Jobs` in that job file. `CreateStep<TStep>` takes the **grain interface**, not the implementation. Its stage overload accepts `JobStepStage`, with implicit integer conversion.

Every step in a stage reaches an outcome before later stages start; steps within a stage run in parallel. All steps are prepared up front, so later-stage preparation cannot read the effects of earlier-stage execution. Read such dependencies in `PerformStep`.

A failed stage stops the pipeline by default: later steps become `Unreachable`, and the job completes with failures. A best-effort stage may override `GetFailureBehaviorFor(JobStepStage)` to return `JobStageFailureBehavior.ContinueWithNextStage` (namespace `Cratis.Orleans.Jobs.Stages`). Do not silently continue when a later stage relies on the failed output.

Requires 1.25.0 for durable step plans: preparation requests must round-trip through the host's JSON options. Invalid plans return `StartJobError.InvalidStepPlan`. Recovery reuses identities and requests, prepares only unsaved steps, and never calls `PrepareSteps` again. Put retry-safe initialization needed before start **and** resume in `OnBeforeDispatchingJobSteps(JobStepDispatchContext)`, after the respective start/resume hook. `InitializationMayBeIncomplete` flags recovery from preparing/starting, not exactly-once initialization. A failed hook dispatches nothing; recovery never replays the start hook. A legacy preparing/starting job without a saved plan returns `CannotResumeJobError.MissingStepPlan` rather than inventing identities.

## 3. Start locally and recover every scope

Requires 1.18.0 for the singleton local service `Cratis.Orleans.Jobs.IJobs`, registered by `AddCratisOrleans`. Prefer `Start<TJob,TRequest>(new JobKey(scope, namespace), request)` (or its explicit job-id overload), `Stop(key, jobId)` and `Delete(key, jobId)` during rolling upgrades. Generic `IJobsManager.Start`/`GetJobsOfType` send types through a manager that may run an older release and cannot decode them.

Inspect `Result<JobId, StartJobError>` before telling a caller that work started. `FailedStartingSomeJobSteps` means remaining work is still running, not that nothing started; `AllJobStepsFailedStarting` means no step started. Start success is not completion. Match `StopJobResult`/`DeleteJobResult`: accepted, `NotFound`, `UnknownJobType`, or `Refused` with the job's error. Unknown types require a release that knows the type; exceptions propagate. Deletion acceptance is not completed removal. Third-party storage should override `IJobStorage.GetJobType` with a metadata-only read; its fallback reads the request too.

`IGrainFactory.GetJobsManager(scope, namespace)` remains available. `IJobsManager.Resume(JobId)` returns **`Task<bool>`**; false means nothing took the job: do not wait for a completion callback as though resumption succeeded. True may also mean already running or finalized. `IJobsManager.Stop` returns `Result<StopJobError>`; preserve a persistence refusal. `Rehydrate()` returns after loading/preparing interrupted jobs, before its bounded background drain resumes them all. `StartStepsInBackground = true` moves step preparation/start to the next turn after durability; later failures belong to the job, not Start.

Requires 1.17.0 for automatic recovery on silo activation/member death. Provide singleton `IJobsManagerKeys` implementations on **every silo**, enumerating all host-owned scope/namespace pairs through `GetKeys(CancellationToken)`. The default remembers only keys used in this process: it is not a durable catalog. Providers run outside request scope; never inject a current tenant/user or scoped database. Requires 1.23.3 (1.24.2 for provider construction failures) for automatic recovery retries of failed providers/managers, from 1 second doubling to 60 seconds until success or silo shutdown. There is no periodic sweep by default. `AddCratisOrleans` registers recovery; separately configured silos need `AddCratisOrleansJobsRecovery()` (`Cratis.Orleans.Hosting`).

Read [lifecycle and placement](references/lifecycle-and-placement.md) for drain/admission, bounded attempts, placement, custom storage compatibility and step test runs.

## 4. Know what storage retains

Successful jobs clear themselves by default; override `KeepAfterCompleted` to retain state. Jobs completed with failures default to `KeepAfterCompletedWithFailures = true`, but retention cleanup still applies.

Keeping a job does not keep successful step state: successful **steps delete their state**. Failed step state is retained. A durable callback receipt, deduplication set or result must live on the **job** (and be persisted), not only on a successful step. Deliver external callbacks to a method on the job's grain interface and preserve `WriteState()`'s `Catch` failure: do not acknowledge a failed write. Step state is not a callback inbox, and a running snapshot does not see later writes automatically.

## 5. Register storage and budget concurrency

`IJobsStorage.GetFor(scope, namespace)` returns `JobsStorage`. Scope and namespace are opaque host-owned storage coordinates, not authorization checks. Choose one storage registration alongside `AddCratisOrleans`:

| Registration | Package / namespace | Use |
| --- | --- | --- |
| `AddCratisOrleansMongoDBJobsStorage` | `.Storage.MongoDB` / `Cratis.Orleans.Setup` | Durable MongoDB; default database `{scope}+jobs[+{namespace}]`, overridable |
| `AddCratisOrleansSqlJobsStorage` | `.Storage.Sql` / `Cratis.Orleans.Setup` | Durable EF Core; `OptionsResolver(scope, namespace)` selects `DbContextOptions<JobsDbContext>` |
| `AddCratisOrleansInMemoryJobsStorage` | `.Storage` / `Cratis.Orleans.Storage` | Specs/development; restart loses state |

The SQL provider calls `dbContext.Database.Migrate()` automatically before constructing storage. Do not substitute `EnsureCreated` or hand-create tables; grant migration permissions and choose scope-to-database mapping deliberately. `IJobsStorage.Reset()` clears the cache; the next resolution migrates again. There is no MySQL/MariaDB jobs provider.

`JobsOptions` properties are **init-only**: supply a constructed instance through the host's options mechanism, not assignment in `Configure(options => …)`.

| Option | Default | Purpose |
| --- | --- | --- |
| `MaxParallelSteps` | `max(1, ProcessorCount - 1)` when unset | Execution throttle per silo |
| `MaxConcurrentStepStarts` | `16` | Startup/resume calls per job, not execution limit |
| `MaxConcurrentRehydration` | `8` | Jobs resumed concurrently by a manager |
| `MaxConcurrentCleanup` | `4` | Dead-job deletions |
| `StepCheckpointBatchInterval` | `100` | Batches between durable checkpoints |
| `StepCheckpointFlushInterval` | `5 seconds` | Timed flush; nonpositive disables it |
| `DeadJobThreshold`, `CleanupCadence` | `1 hour` each | Preparation-with-no-steps scavenging; saved plans are excluded |
| `TerminalJobRetention` | `7 days` | Terminal history; `Timeout.InfiniteTimeSpan` retains indefinitely |
| `MaxTerminalJobsPerCleanup` | `100` | Bounded oldest-first cleanup page |

Concurrency values below one become one. Retention does not delete preparing/running/stopped/removing jobs. Larger checkpoint intervals increase post-crash redelivery, not delivery guarantees.

## State machines and verification

Use [state-machines.md](references/state-machines.md) for `StateMachine<TStoredState>` and `EventSourcedStateMachine<TModel>`. Neither makes external effects exactly-once.

Verify planning, invalid/missing saved plans, idempotent redelivery, stop/resume/refusal, failed barriers/Unreachable, retention and persistence failures. Exercise durable storage restart recovery and SQL migration permissions. A build alone cannot establish crash recovery. Do not operate a live store merely to validate an example.
