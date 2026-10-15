---
name: cratis-orleans-jobs
description: Build recoverable work with Cratis.Orleans jobs and job-step grains, stage barriers, bounded startup and cleanup, storage scopes and state machines. Use for service or kernel job authors, job recovery, SQL schema setup, or grain lifecycle transitions. Framework guidance; do not use for operating live Chronicle jobs or for application command validation.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-orleans-jobs/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Recoverable Orleans jobs

For services and kernel contributors using Orleans. This is library-consumer guidance, not the application vertical-slice architecture. For live Chronicle operations use **cratis-chronicle-cli-operations** instead. Co-hosting conventions are in [orleans.md](../../rules/orleans.md).

## Verified product sources

All Orleans claims below are source reads from **Cratis/Orleans v1.16.3**.

| Packages | Version | Sources |
| --- | --- | --- |
| `Cratis.Orleans`, `Cratis.Orleans.Storage` | `1.16.3` | `Source/Orleans/Jobs/{Job.cs,Job.Steps.cs,Job.Stages.cs,JobStep.cs,IJobsManager.cs,JobsOptions.cs}`; `Source/Orleans.Storage/Jobs/*` |
| `Cratis.Orleans.Storage.MongoDB`, `Cratis.Orleans.Storage.Sql` | `1.16.3` | `Source/Orleans.Storage.MongoDB/{MongoDBJobsStorageOptions.cs,DatabaseNames.cs}`; `Source/Orleans.Storage.Sql/{SqlJobsStorage.cs,SqlJobsStorageOptions.cs,Jobs/Migrations/v1_0_0.cs}` |
| `Cratis.Orleans.Chronicle` | `1.16.3` | `Source/Orleans.Chronicle/StateMachines/EventSourcedStateMachine.cs:19-40,77-94,128-176`; [state-machine reference](references/state-machines.md) |

The API snippets cite their exact files and lines below. Product docs `Documentation/concepts/{jobs,storage-scopes,state-machines,event-sourced-state-machines}.md` supply the task vocabulary; source resolves contradictions.

## 1. Choose a job, not a detached task

A job plans recoverable work; each step is a grain and does the work. Use it for work that must outlive a request. It is **at-least-once**, not exactly-once: a step interrupted before a durable checkpoint can perform the same work again. Protect external effects with receiver-owned idempotency/deduplication; a checkpoint does not atomically commit an external write (`JobStep.cs:47-56`, `JobsOptions.cs:34-59`).

- Job: derive `Job<TRequest, TJobState>`; the request is a class implementing `IJobRequest`, the state derives `JobState` (`Job.cs:23-25`). Implement `PrepareSteps(TRequest)` returning `Task<IImmutableList<JobStepDetails>>` (`Job.Steps.cs:63-87`).
- Step: derive `JobStep<TRequest, TResult, TState>`; state derives `JobStepState`. Its constructor receives `[PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<TState>`, `IJobStepThrottle`, and `ILogger<JobStep<TRequest, TResult, TState>>` (`JobStep.cs:27-32`).
- Implement `PrepareStep(TRequest)` → `Task<Result<PrepareJobStepError>>`; `InitializeState(TRequest)` → `ValueTask`, called after preparation succeeds; and `PerformStep(TState, CancellationToken)` → `Task<Catch<JobStepResult>>` (`JobStep.cs:350-387`). `TResult` is the step's result type; the work method returns the framework's `JobStepResult` envelope, not `TResult` directly.
- Give the concrete job and step unique `[GrainType("product-specific-name")]` names. Use serializable request/state/result types. Do not reuse a class-derived grain name across assemblies (`Jobs/JobsManager.cs:31`, `Documentation/how-to/host-the-silo.md`, and `orleans.md`).

`PerformStep` runs **outside the activation context** with a state snapshot obtained through the grain reference. Mutating that snapshot does not persist it; do not access/write grain `State` directly from background work (`JobStep.cs:375,390-450`). Keep progress/state coordination on grain calls. Since 1.16.2 the framework owns stop reporting; do not report cancellation again from a `PerformWork` override (`:390-450`, `Documentation/concepts/jobs.md:117-120`).

## 2. Plan dependencies as stages

Default steps all use `JobStepStage.First`. Prefer independent fan-out. Use stages only when correctness requires earlier work to finish (`Job.Stages.cs:20-44`).

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

Import `System.Collections.Immutable` and `Cratis.Orleans.Jobs` in that job file. `CreateStep<TStep>` takes the **grain interface**, not the implementation (`Job.Steps.cs:69-79`). Its stage overload accepts `JobStepStage`, with implicit integer conversion (`Source/Orleans.Storage/Jobs/Stages/JobStepStage.cs`).

Every step in a stage reaches an outcome before later stages start; steps within a stage run in parallel. All steps are prepared up front, so later-stage preparation cannot read the effects of earlier-stage execution. Read such dependencies in `PerformStep` (`Job.Stages.cs:27-33`).

A failed stage stops the pipeline by default: later steps become `Unreachable`, and the job completes with failures. A best-effort stage may override `GetFailureBehaviorFor(JobStepStage)` to return `JobStageFailureBehavior.ContinueWithNextStage` (`Job.Stages.cs:40-44,152-194`, namespace `Cratis.Orleans.Jobs.Stages`). Do not silently continue when a later stage relies on the failed output.

## 3. Start and resume through the scoped manager

`IGrainFactory.GetJobsManager(scope, namespace)` resolves the manager (`JobsManagerExtensions.cs:18-19`). `IJobsManager.Start<TJob,TRequest>(request)` returns `Result<JobId, StartJobError>`; inspect its error before telling a caller that a job started (`IJobsManager.cs:37-52`). Start success is not completed work; observe the job's status/outcomes.

`IJobsManager.Resume(JobId)` returns **`Task<bool>`**. False means nothing took the job; do not wait for a completion callback as though resumption succeeded. True can mean resumed, already running, or finalized as completed (`IJobsManager.cs:54-64`). `Rehydrate()` completes after loading/preparing interrupted jobs, **before** its bounded background drain has resumed them all (`:15-28`).

`StartStepsInBackground` defaults to false. Overriding it to true makes Start return once the job is durable, moving step preparation/start to the grain's next turn. Later startup errors belong to the job, not the Start response (`Job.cs:74-87,395-427`).

## 4. Know what storage retains

Successful jobs clear themselves by default; override `KeepAfterCompleted` to retain job state. Jobs completed with failures default to `KeepAfterCompletedWithFailures = true`, but retention cleanup still applies (`Job.cs:58-71`, `JobsOptions.cs:80-97`).

**Keeping a job does not keep successful step state.** The step provider deletes successful/removing state, moves failed state to failed-step storage, and saves stopped/scheduled/running/unreachable state (`JobStepGrainStorageProvider.cs:64-84`). SQL keeps failed step rows with a failure status rather than a separate collection (`Source/Orleans.Storage.Sql/Jobs/JobStepStorage.cs`, `MoveToFailed`). Copy results the job must retain through `OnStepCompletedOrStopped(JobStepId, JobStepResult)` into job state (`Job.Steps.cs:49-55,110-117`); do not read deleted successful step state later.

Deliver external callbacks to a method on the **job's grain interface**, update job state there and persist it with `WriteState()`. Preserve the returned `Catch` failure; do not acknowledge input whose write failed (`Job.State.cs:13-25`, `WriteState`; `Documentation/concepts/jobs.md:122-164`). Step state is not a callback inbox: a concurrent callback can race the final write, and successful completion deletes it. A running step's snapshot will not see later job/step writes automatically.

## 5. Register storage and budget concurrency

`IJobsStorage.GetFor(scope, namespace)` returns `JobsStorage`. Scope and namespace are opaque engine coordinates carried in grain keys; the host/provider owns their mapping (`Source/Orleans.Storage/Jobs/IJobsStorage.cs`, `Job.cs:120-125`). They are storage boundaries, not authorization checks.

Choose one storage registration alongside `AddCratisOrleans`:

| Registration | Package / namespace | Use |
| --- | --- | --- |
| `AddCratisOrleansMongoDBJobsStorage` | `.Storage.MongoDB` / `Cratis.Orleans.Setup` | Durable MongoDB; database-name resolver can override the default `{scope}+jobs[+{namespace}]` |
| `AddCratisOrleansSqlJobsStorage` | `.Storage.Sql` / `Cratis.Orleans.Setup` | Durable EF Core; `OptionsResolver(scope, namespace)` selects typed `DbContextOptions<JobsDbContext>` |
| `AddCratisOrleansInMemoryJobsStorage` | `.Storage` / `Cratis.Orleans.Storage` | Specs/development only; process restart loses state |

Evidence: each package's `*JobsStorageServiceCollectionExtensions.cs`; MongoDB `DatabaseNames.cs` and `MongoDBJobsStorageOptions.cs`; SQL `SqlJobsStorageOptions.cs:15-27`; in-memory `InMemory/InMemoryJobsStorage.cs`.

### SQL schema contradiction resolved

The product's SQL setup docs say to create the schema yourself. **At v1.16.3 the provider applies migrations automatically**: `SqlJobsStorage.GetFor` calls `EnsureSchema`, which calls `dbContext.Database.Migrate()` before constructing storage (`Source/Orleans.Storage.Sql/SqlJobsStorage.cs:33-49,58-74`). The migration is `Jobs/Migrations/v1_0_0.cs`; do not substitute `EnsureCreated` or hand-create those tables. The connection needs permissions to apply the migration, and scope-to-database mapping must be deliberate. Storage is cached per scope/namespace; `IJobsStorage.Reset()` clears the cache and the next resolution migrates again (`SqlJobsStorage.cs:52-56`). This resolves the documentation conflict from source, not from a release-note inference.

Configure `JobsOptions` through the host's options registration. Its properties are **init-only**: supply a constructed instance via the options mechanism rather than assigning them in a `Configure(options => options.Property = …)` callback (`JobsOptions.cs`).

| Option | Default | Purpose |
| --- | --- | --- |
| `MaxParallelSteps` | `max(1, ProcessorCount - 1)` when unset | Actual execution throttle |
| `MaxConcurrentStepStarts` | `16` | Startup/resume calls per job, not the execution limit |
| `MaxConcurrentRehydration` | `8` | Jobs resumed concurrently by a manager |
| `MaxConcurrentCleanup` | `4` | Dead-job deletions |
| `StepCheckpointBatchInterval` | `100` | Batches between durable progress checkpoints |
| `StepCheckpointFlushInterval` | `5 seconds` | Timed checkpoint flush; nonpositive disables it |
| `DeadJobThreshold`, `CleanupCadence` | `1 hour` each | Preparation-with-no-steps scavenging |
| `TerminalJobRetention` | `7 days` | Terminal history retention; `Timeout.InfiniteTimeSpan` retains indefinitely |
| `MaxTerminalJobsPerCleanup` | `100` | Bounded oldest-first cleanup page |

Source: `JobsOptions.cs:17-109`. The three concurrency options for starts/rehydration/cleanup treat values below one as one. Retention does not delete preparing/running/stopped/removing jobs. Larger checkpoint intervals mean more post-crash redelivery, not higher delivery guarantees.

## State machines and verification

Use [state-machines.md](references/state-machines.md) for `StateMachine<TStoredState>` and `EventSourcedStateMachine<TModel>`: jobs are recoverable work, state machines are lifecycle behavior. Neither makes external effects exactly-once.

Verify a new job with specs for planning, idempotent redelivery, stop/resume (including false), failed barriers/Unreachable, retention and persistence failures. Exercise the selected durable provider for restart recovery and SQL migration permissions. A build alone cannot establish crash recovery. Do not operate a live store merely to validate a code example.
