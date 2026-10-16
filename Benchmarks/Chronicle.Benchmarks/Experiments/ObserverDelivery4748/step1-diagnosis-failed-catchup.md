# Diagnosis — failed 2k catch-up job (row 42) and the 25 s gap (row 34)

Cratis/Chronicle#4748, step 1. Engine `d5d0eea66d` (image `cratis/chronicle:bench-4748`). Harness `e11318b106` on `chronicle-4748-measure`. Source/** was only read.

## Verdict

**An engine defect that only shows under load. The harness did not cause it.** A forced reproduction (Orleans response timeout lowered to 5 s; section "Forced reproduction" below) confirms it, with the exception text. Under that setting the outcome is worse than row 42: the job is left `Running` forever with orphaned steps, which is the #4665 stall.

Each catch-up step makes one call to the observer grain, and those calls queue up one behind another. The queue grows with the number of partitions. When draining it takes longer than the Orleans 30 s response timeout, the steps at the back fail on the caller side. The client had already handled their events. These failures:
- are not recorded as failed partitions (`failedPartitionCount` 0), so there is no retry or recovery;
- undercount `handledEventCount`;
- leave the job stored forever with status 6 (CompletedWithFailures). The job-level `statusChanges` carry no exception text, because step errors live in the step state.

Row 34's 25 s effect→durable gap comes from the same serial queue. It drained just under the limit.

Row 42 was a harness artifact in one respect only: `runningState 4` (Disconnected) was most likely read after the client was disposed (see "Open").

## Evidence

**1. Every step calls one observer grain, and those calls run one at a time.**
- `HandleEventsForPartition.PerformStep` (Source/Kernel/Core/Observation/Jobs/HandleEventsForPartition.cs:245-250): after the client answers Ok, it calls `ReportNewSuccessfullyHandledEvent`, then `await _observer.ReportHandledEvents(partition, counts)`. The `_observer` is the single `Observer` grain.
- `IObserver.ReportHandledEvents` (Core/Observation/IObserver.cs:64) is **not** `[AlwaysInterleave]`, so Orleans queues the calls. The grain also holds `_alertMutationLock` for the whole call and does two Mongo writes per call: `observer-handled-counts` Increment and `WriteStateAsync` (Observer.Handling.cs:29-50).
- `Job.OnStepSucceeded` on the single Job grain is a second serial point: about 4004 `jobs` updates per 2k run.
- In the debug rerun (row 50), the kernel log shows all 2000 steps reaching `Running` by 20:19:51.58. Before that, all 2000 steps acquired a throttle slot within 2.4 s, so the throttle never held them back. The client finished all effects at about 20:19:51.5. "completed successfully all events" (logged after `ReportHandledEvents`) then arrives at a flat **~230–250 per second** until 20:20:08.24. That is a single-server queue at about 4 ms per call, 17 s after the last client effect.
- Harness profiler counts for each 2k run: `observer-handled-counts` updates = number of `ReportHandledEvents` calls that executed. `observers` updates are about the same. `failed-job-steps` removes = successful steps.

**2. The Orleans response timeout is 30 s for every grain call.** `Clustering.ResponseTimeout = 30s` (Core/Configuration/Clustering.cs:86) is applied to `SiloMessagingOptions.ResponseTimeout`, `SystemResponseTimeout` and `ClientMessagingOptions` (Server/Program.cs:199-205).

**3. Row 42's job crossed 30 s, row 34 got close, and today's runs stayed under:**

| Run | Steps start (status 3) | Running (status 4) | Last step / completion | Start→done | Result |
|---|---|---|---|---|---|
| row 42 | 19:46:17.094 | 19:46:21.430 | 19:46:52.085 (status 6) | 35.0 s (30.7 s from Running) | 38 failed |
| row 34 | 19:19:24.144 | 19:19:28.459 | 19:19:53.757 (status 4 write) | 29.6 s (25.3 s from Running) | success, 25.4 s gap |
| row 49 (rerun 1) | — | — | durable at 17.9 s from clock | gap 12.2 s | success |
| row 50 (rerun 2, debug) | 20:19:49.165 | 20:19:51.581 | 20:20:08.240 | 19.1 s | success, gap 16.8 s |

Row 42 ran while the host was loaded: seeding took 17.5 s, compared with 8–9 s in rows 49/50 and 13.9 s in row 34. So each Mongo write in the serial queue was slower.

**4. The numbers in row 42 fit callers timing out at 30 s.**
- 1962 steps succeeded and 38 failed.
- `observer-handled-counts` saw 1967 updates, matching the persisted `handledEventCount` of 1967. `observers` saw 1968 updates.
- So 33 `ReportHandledEvents` calls never ran. 5 more ran, but their steps still failed.

This matches 38 callers timing out after 30 s. 33 of those requests expired in the grain queue and were never executed. 5 completed after their caller had already given up, or the step failed afterwards on the Job grain call. That would be `JobStep.HandleJobStepResult` writing "Error reporting job step succeeded", Cratis.Orleans v1.16.3 Source/Orleans/Jobs/JobStep.cs:485-492.

**5. Why exceptions are missing and partitions are not marked failed.** A timeout thrown by `ReportHandledEvents` or `ReportNewSuccessfullyHandledEvent` reaches `catch (Exception e)` at HandleEventsForPartition.cs:317-326. Because `lastSuccessfullyHandled` was already set (line 246), that path returns `FailedWithPartialResult` **without** calling `_observer.PartitionFailed`. Only the `failed` branch at lines 288-292 calls it. So `failedPartitionCount` stays 0.

The step's error is stored in the step state (`failed-job-steps`), not in the job. `Job.OnStepFailed` only increments `Progress.FailedSteps`, and the job's status-6 entry is written with empty `exceptionMessages` (Cratis.Orleans v1.16.3 Source/Orleans/Jobs/Job.Steps.cs:40-44, Job.cs:568). `KeepAfterCompletedWithFailures => true` (Job.cs:71) keeps the job forever. `CatchUpObserver.OnAllStepsCompleted` still calls `CaughtUp(..., lastScanned: Unavailable)` (CatchUpObserver.cs:69-72). The observer is at next 2000 / lastHandled 1999, `handledAllEvents` true.

**6. The client stayed connected while the job ran.**
- `catchingUp` is a `using` local inside `Probe.Measure` and lives until Measure exits at the 320 s measurement deadline. That was around 19:50:3x, minutes after the job finished at 19:46:52.
- Row 42's log has no client Warning+ lines (the client logs to the console at Warning+), so the reactor stream was not lost or reconnected.
- The outbound path was fine: all 2000 events reached the client exactly once.
- The other failure modes are ruled out. A subscriber timeout or disconnect (`SubscriberCallTimedOut`, `ObserverSubscriberState.Disconnected`) goes through the `failed` branch and would have set `failedPartitionCount` > 0.

**7. Reruns** (the new harness captures kernel logs, failed job steps, failed partitions, connected-state snapshot and per-event first-delivery times):
- Rerun 1 (`diagnose-2k-r1-a2.log`, row 49; first attempt `diagnose-2k-r1.log` exited 75 in the admission queue): COMPLETE, exact 2000/2000, gap 12.2 s. The kernel log has no warnings after startup.
- Rerun 2 (`diagnose-2k-r2-debug.log`, row 50, `PROBE_KERNEL_DEBUG=1`): COMPLETE, exact 2000/2000, gap 16.8 s. The debug step timeline is above (`measure4748-f1727080…-kernel.log`).
- Neither rerun failed, so there are no failed step ids to match against kernel log lines. The failure did not come back on a less loaded host. That is expected from the mechanism and is why the exact exception text is still unproven.

## Forced reproduction (harness `efa26edae2`, `PROBE_KERNEL_RESPONSE_TIMEOUT=00:00:05`, `PROBE_KERNEL_DEBUG=1`)

The new opt-in env var sets `Cratis__Chronicle__Clustering__ResponseTimeout` on the kernel container. The row records it as `kernelResponseTimeout`, default "default (Clustering.ResponseTimeout 00:00:30)". Build `build-repro.log` exit 0. I ran two attempts of `catchup:reactor:2000:2000:64`; neither waited in the admission queue.

| Attempt | Row / store | Client | Job | Observer while connected |
|---|---|---|---|---|
| 1 `repro-5s-r1.log` | row 51 / `…d5be9065` | exact 2000/2000, 0 duplicates, effect 9.1 s | status 4 Running forever: 115 succeeded / 678 failed / 1207 unaccounted. Stopped only at client disposal (20:30:55) | not captured (harness only snapshots on status ≥6); final read after disposal: runningState 1, handled 265, failedPartitionCount 0 |
| 2 `repro-5s-r2.log` | row 52 / `…4bb76267` | exact 2000/2000, 0 duplicates, effect 3.7 s | status 4 Running for ≥4 min: 396 succeeded / 535 failed / **1092 step documents stuck at status 2 (Running)**. Stopped at client disposal (20:38:27) | read-only poller `repro-5s-r2-poll/poll.jsonl` (docker exec mongosh every 15 s): runningState **1 (Active)**, handledEventCount 477, **failedPartitionCount 0**, failed-partitions 0, **catchingUpPartitions 2000, next 0, lastHandled unset**, no `RetryFailedPartition` jobs. Unchanged from 20:34:53 to 20:38:20 |

**Exception text.** It is stored on the step in `failed-job-steps` (status 4 CompletedWithFailure) and repeated in the kernel Warning "Job step 'HandleEventsForPartition' failed while performing job step". The job document itself still carries none:
> `System.TimeoutException: Response did not arrive on time in 00:00:05 for message: Request [... sys.client/hosted-...]->[... observer/probe-4748#<store>#Default#event-log] Cratis.Chronicle.Observation.IObserver.ReportHandledEvents(Key, IReadOnlyDictionary<EventTypeId,EventCount>)` … `at Cratis.Chronicle.Observation.Jobs.HandleEventsForPartition.PerformStep(...)`, followed by "HandleEventsForPartition job step failed, but it had successfully handled some events. Last successfully handled event was: N".

**Which grain call timed out.** Counted from the kernel logs. Each exception is logged twice, so these are occurrence counts.

| Call target | Attempt 1 | Attempt 2 |
|---|---|---|
| `IObserver.ReportHandledEvents` | 3762 | 3206 |
| `IJob.OnStepFailed` (Job grain) | 1230 | 1092 |
| `IJobStep.ReportFailure` (step to itself, which then calls the job) | 1228+9 | 1068+29 |
| `IJob.OnStepSucceeded` | 2 | 0 |
| any other `IObserver` method | 0 | 0 |

So the primary failure is the **observer** call: `ReportHandledEvents` is non-interleaved and holds a lock while doing two writes per call. Every handled event's step fails at that call, after the client has already returned Ok. Then the **job** grain, flooded by the burst of step reports, times out on `OnStepFailed`. That failure goes to "Failed to report that that the performing of job step was cancelled. Error: WorkerError" (Cratis.Orleans JobStep.cs ReportError path). The job never learns about those steps, their documents stay at `Running`, `OnAllStepsCompleted` / `CaughtUp` never run, and the observer stays with 2000 catching-up partitions at next 0.

**Retry behaviour.**
- No retries happened: `failedPartitionCount` stayed 0, `failed-partitions` was empty, and no `RetryFailedPartition` jobs were created. The ReportHandledEvents timeout takes the catch path at HandleEventsForPartition.cs:317-326, which never calls `PartitionFailed`.
- Nothing recovers the job while the client stays connected.
- When the client disconnects, the job goes to `Stopped` with its step documents still `Running`. That is the state #4665 (orphaned Running steps) and #4363 (Stopped job) describe on resubscribe.
- Not tested: whether live events for the 2000 catching-up partitions are dropped meanwhile.

**Why row 42 ended at status 6 instead of stalling.** At 30 s, only the observer queue (~31–35 s) overran. The job grain's queue stayed under the limit, so every failure report reached the job and it completed with failures. At 5 s both queues overran. Without the forced setting, the same stall should appear once the job-grain queue also exceeds 30 s: larger catch-ups, or more load.

**Running state.** While the client was connected, the observer was **Active (1)**, not Disconnected. Row 42's `runningState 4` is therefore attributed to the post-disposal read, which is a harness artifact, though still not directly observed for row 42.

## Relation to existing issues

- **#4665** (open) describes the *consequence* reproduced here: a catch-up job left `Running` with orphaned steps stalls its observer for good and swallows failed-partition retries. Its production root cause was #4664 (subscriber timeouts while the client works through deliveries one at a time) "but any cause that leaves steps orphaned leads to the same permanent stall".
- **#4664** (open) covers that different trigger: the client's queue plus `OnNextWithin`'s 30 s timeout. Here the handler is trivial: all effects are done within 3.7–9 s and no `SubscriberCallTimedOut` occurs.
- **#4363** (open) covers a `Stopped` job whose steps are all finished. Its suggested fix 3 already notes "never records a failed partition".
- None of the three names this trigger: the bookkeeping calls each step makes after a successful delivery (observer `ReportHandledEvents`, then job `OnStepFailed` / `OnStepSucceeded`) serialize on single grains and overrun the Orleans response timeout, purely as a function of partition count × per-call latency.

Recommendation: file **one new issue** for the root cause (below) and link it from #4665 with a short comment (below). Do not fold it into #4664: the trigger and the fix differ.

## Still open / not proven

- Row 42's own exception text. The container is gone; the forced runs give the same `ReportHandledEvents` timeout at 5 s.
- That a 30 s stall (not just status 6) appears naturally at larger N, e.g. 10k/10k (not run).
- Live-event behaviour while the job is stalled, and behaviour on resubscribe (not run).
- The harness's `JOB_FAILED` capture path still only fires on status ≥6. A job stuck at status 4 is only visible through the external poller. Extending the harness to treat "unchanged for N s" as terminal would need another rebuild (not done).

## Reproduction recipe

```sh
cd /Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure   # harness efa26edae2, engine d5d0eea66d
PATH=/opt/homebrew/bin:$PATH PROBE_KERNEL_RESPONSE_TIMEOUT=00:00:05 PROBE_KERNEL_DEBUG=1 \
CRATIS_CHRONICLE_LOCAL_IMAGE=cratis/chronicle:bench-4748 BENCH_MONGO_PORT=27448 BENCH_CHRONICLE_PORT=35448 \
PROBE_OUT=<out> PROBE_DEADLINE_SECONDS=420 PROBE_DRAIN_SECONDS=180 PROBE_FINALIZE_SECONDS=100 \
dotnet Benchmarks/Chronicle.Benchmarks/bin/Debug/net10.0/Chronicle.Benchmarks.dll probe catchup:reactor:2000:2000:64
```

Expected result (2/2 attempts): the client gets 2000/2000 exactly once; the job stays at status 4 with steps stuck `Running`; observer Active, `failedPartitionCount` 0, catchingUpPartitions 2000. To observe this while the client is connected, run `repro-5s-r2-poll/poll.sh` alongside. At the default 30 s timeout, row 42 (a loaded host) gave status 6 with 38 failed steps.

## Draft issue (final — for the orchestrator to file; not filed)

**Title:** Catch-up steps fail after successful delivery when per-step `ReportHandledEvents` / job reports queue past the Orleans response timeout, leaving the job Running with orphaned steps

**Describe the bug**
Each `HandleEventsForPartition` step makes the same calls after the subscriber answers Ok:
- `ReportNewSuccessfullyHandledEvent` (to itself);
- `IObserver.ReportHandledEvents` (HandleEventsForPartition.cs:245-250);
- at completion, `IJob.OnStepSucceeded` or `IJob.OnStepFailed`.

These calls end up queued on single grains:
- `ReportHandledEvents` is not `[AlwaysInterleave]` (IObserver.cs:64). It holds `_alertMutationLock` and does an `observer-handled-counts` increment plus a full `WriteStateAsync` per call (Observer.Handling.cs:29-50).
- The CatchUpObserver Job grain writes its state twice per step.

During a catch-up over many partitions, all steps run at once, and these calls drain at about 4–15 ms each. Once the queue is longer than the Orleans `ResponseTimeout` (`Clustering.ResponseTimeout`, default 30 s, applied in Server/Program.cs:199-205), callers time out:

1. A step whose events the client **already handled** throws `TimeoutException` from `ReportHandledEvents`. The catch at HandleEventsForPartition.cs:317-326 returns `FailedWithPartialResult` **without `PartitionFailed`**: no failed partition, no retry, and an undercounted `handledEventCount`.
2. The step's failure report then times out on `IJob.OnStepFailed` ("Failed to report … Error: WorkerError"). The job never accounts for the step, the step document stays `Running`, and `OnAllStepsCompleted` / `CaughtUp` never run. The observer stays Active with every partition in `catchingUpPartitions` and next 0, which is the permanent stall described in #4665. On client disconnect the job goes to `Stopped` with Running step documents (#4363 / #4665 territory).
3. If only the observer queue overruns, the job instead completes `CompletedWithFailures`. The failed steps carry the exception text only in `failed-job-steps`; the job's `statusChanges` have none. The job is retained (`KeepAfterCompletedWithFailures`).

**Observed** (engine d5d0eea66d, `cratis/chronicle:bench-4748`, one silo, reactor with a no-op handler, 2000 events on 2000 partitions):
- Default 30 s on a loaded host: job status 6, 1962 succeeded / 38 failed. `handledEventCount` 1967, `failedPartitionCount` 0. Client 2000/2000 exactly once.
- Default 30 s on an unloaded host: success, but durable completion lags the last client effect by 12–25 s. Step completions arrive at a flat ~240/s, the single-grain drain rate.
- `Cratis__Chronicle__Clustering__ResponseTimeout=00:00:05`, 2/2 attempts: job stuck `Running`, attempt 2 at 396 succeeded / 535 failed / 1092 steps left `Running`. Observer Active, catchingUp 2000, next 0, `failedPartitionCount` 0, no retry jobs. Client 2000/2000 exactly once. Step error: `TimeoutException: Response did not arrive on time in 00:00:05 for message: … observer/probe-4748… IObserver.ReportHandledEvents(...)`. Kernel log counts per attempt: ~1600–1900 `ReportHandledEvents` timeouts and ~550–615 `IJob.OnStepFailed` timeouts.

**Expected**
- Bookkeeping after an Ok delivery cannot turn a handled step into a failed one.
- Any step failure is recorded as a failed partition (so it is retried) and its reason is visible on the job.
- A step failure that cannot be reported does not leave the job `Running` forever.
- Catch-up completion time does not grow linearly through one non-reentrant grain call per partition.

**Steps to reproduce**
Benchmarks probe on branch `chronicle-4748-measure` (harness `efa26edae2`):
- `probe catchup:reactor:2000:2000:64` with `PROBE_KERNEL_RESPONSE_TIMEOUT=00:00:05`. This is deterministic.
- At the default timeout, a larger catch-up (e.g. 10k partitions) should exceed 30 s (not yet run).
- Any reactor or projection catch-up over enough distinct partitions behaves the same way.

**Suspected code**
- HandleEventsForPartition.cs:245-250 (per-step `ReportHandledEvents`) and :317-326 (timeout after partial success becomes a failure with no `PartitionFailed`).
- IObserver.cs:64 / Observer.Handling.cs:29-50 (serialized call, two writes per call).
- Cratis.Orleans v1.16.3 `JobStep.HandleJobStepResult` / `ReportError` (a failed report to the job leaves the step `Running` and the job unaware) and `Job.OnStepSucceeded` / `OnStepFailed` (serialized per step).

Possible directions:
- Aggregate handled counts per job, or batch them, instead of making one observer call per step. Or make the call interleavable or one-way.
- Treat a bookkeeping timeout after Ok as success with accounting to retry.
- Make step-to-job reporting durable or retried, and have the job reconcile steps whose documents are terminal or stale.

**Related:** #4665 (the resulting stall), #4664 (a different trigger of step timeouts), #4363 (Stopped variant; its fix 3 also covers a step that never records a failed partition).

### Comment to add on #4665 (not posted)

> Another trigger for the orphaned-`Running`-steps state, reproduced without slow handlers: each catch-up step makes a non-interleaved `IObserver.ReportHandledEvents` call, and then reports to the Job grain. On a large catch-up those calls queue on single grains past the Orleans response timeout. Steps whose events were already handled fail with `TimeoutException … IObserver.ReportHandledEvents`, and no `PartitionFailed` is recorded. Their `IJob.OnStepFailed` reports then time out too, so the step documents stay `Running` and the job never completes.
>
> Deterministic repro: 2000 partitions, no-op reactor, `Clustering.ResponseTimeout=5s`. Result in 2/2 attempts: job stuck `Running` with 1092 Running steps, observer Active with 2000 catching-up partitions, `failedPartitionCount` 0, no retry jobs. At the default 30 s on a loaded host we saw a `CompletedWithFailures` job with 38 failed steps.
>
> Details and root cause in #<new issue>.
