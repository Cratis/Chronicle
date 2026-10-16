# Diagnosis — results.jsonl row 59 `catchup:projection:10000:100:64` (store measure4748-39471ec9…)

Read-only diagnosis. Nothing was built, run or edited apart from this note. Harness worktree: `.worktrees/chronicle-4748-measure` @ 7fa5eb4497. Kernel jobs base: `Cratis/Orleans` tag v1.16.3, which matches the `Cratis.Orleans` 1.16.3 pin in `Directory.Packages.props:79`.

## Verdicts

| Question | Verdict |
|---|---|
| 1. Is the job-completion check wrong for projections? | **Yes. Harness bug.** The check asks for evidence that the kernel never persists for the last step to finish. A single-step projection job always fails it. |
| 2. Why don't projection values match? | **Harness timing artifact.** The documents snapshot is from the previous loop iteration, taken before the catch-up step finished. There is no loss and no out-of-order application, and nothing shows the projection lagging after tail. |

## 1. Job completion — evidence

- **Projections use one step.** `CatchUpObserver.CreateStepsFor` (`Source/Kernel/Core/Observation/Jobs/CatchUpObserver.cs:145-160`) creates a single `HandleEventsForObserver` step for `ObserverType.Projection`. A reactor gets one `HandleEventsForPartition` step per key. Same job type (`CatchUpObserver`), same retention, and the job is still removed on success.
- **The final write happens before the last step's result is recorded.** In Orleans v1.16.3 `Job.Steps.cs`:
  - `OnStepSucceeded` increments `SuccessfulSteps` (line 25).
  - `PerformStepEventHandling` then calls **`WriteState()`** (line 93).
  - Only after that does `OnStepCompletedOrStopped` run (line 114). That is where `JobStateWithLastHandledEvent.HandleResult` sets `HandledAllEvents=true` and `LastHandledEventSequenceNumber` (`JobStateWithLastHandledEvent.cs:82-108`).
  - `HandleCompletion` → `ClearStateAsync()` (`Job.cs:579`) removes the job document with no further write (`ClearedState` ⇒ `needsToWriteState=false`, `Job.Steps.cs:152-159`).
- **So the last persisted update never contains the final step's watermark.**
- **What the raw BSON shows (row 59 `jobWriteSamples`):**
  - The update at 21:41:25.372Z has `status 4 (Running)`, `progress {totalSteps 1, successfulSteps 1, failed 0}`, `handledAllEvents false`, and `lastHandledEventSequenceNumber 18446744073709551615 (Unavailable)`.
  - It is followed by a `remove` with `ndeleted 1` at 21:41:25.382Z.
  - The observer state was at tail: `handledEventCount 10000`, `lastHandled 9999`, `next 10000`, no catching-up or in-flight partitions, `failedPartitions []`.
- **Why the check fails.** `ProbeMongo.CompletedCatchupJobs` (`ProbeMongo.cs:197-199`) requires `handledAllEvents && last >= tail && last < Max-2`. The job never satisfies that, so `completed.Length == 0` and Probe throws at `Probe.cs:268`.
- **Reactor runs passed by ordering luck.** In rows 57 and 58 the 100th write carries the results of the first 99 steps. It would fail the same way if the partition that holds the tail event finished last.
- **Row 13 (projection smoke, 200/20) has the identical shape:** `totalSteps 1`, `handledAllEvents false`, `last = Max`. It was accepted only because `handledAllEvents` was added to the check later (commit 44ba16e194, 15:46; row 13 ran at 14:58).

## 2. Projection values — evidence

The comparison is between `projectionDocuments` and the latest event per key in `measure4748-39471ec9…-expected.json` (10000 entries, sequence numbers 0–9999, 100 keys).

- **49 of 100 rows mismatch on `Value`.** For each of those keys:
  - The latest event has a sequence number in **9951–9999**, and those 49 keys are exactly the ones that mismatch.
  - The document holds that key's **previous** event: both `Value` and `__lastHandledEventSequenceNumber` match the second-latest entry in the expected file, 49 of 49. Examples:
    - `7dcc482f…`: expected 9952, actual 9852 (sequence 9852).
    - `b5991aba…`: expected 9953, actual 9853 (sequence 9854).
- **The other 51 rows match exactly.**
- **Name and Timestamp are not the cause.** `Name` mismatches: 0. `Timestamp` mismatches under the harness's millisecond truncation: 0.
- **Across all 100 documents, the highest sequence number is 9950.** The sink was a clean prefix through 9950: no gaps, no stale values and no reordering.
- **Why the snapshot is stale.** `ProjectionRows` and `ProjectionMatches` (`Probe.cs:286-293`) run *after* the tail/completion branch (`Probe.cs:263-268`). In the iteration that reached tail, the throw at line 268 meant the documents were never re-read. The recorded snapshot is therefore from the previous iteration, when the job was still running.
- **The sink did eventually receive every event.** The end-of-run profile (`mongo.rows`) shows 10000 `update` commands on `BenchmarkProjectionModels`, with `documentsModified` 9900 plus 100 first writes.
- **No evidence of lag after tail.** Lag is not proven, and nothing in the row shows it.

## Exact harness fix (not applied)

1. **`ProbeMongo.cs:199`.** Drop `!job["handledAllEvents"].AsBoolean || last < tail || last >= (decimal)(ulong.MaxValue - 2) ||` together with the `last` variable on line 196. Keep:
   - the type and key checks,
   - `successfulSteps == totalSteps > 0`,
   - failed, stopped and unreachable all 0,
   - the deletion after the write.

   The tail is already proven by the observer's persisted state, `StateAtTail(state, tail, ProbeObserverId)` at `Probe.cs:263`, which is the kernel's `CaughtUp` result. Update the exception text accordingly. This applies to both reactors and projections.
2. **`Probe.cs`.** Move the `if (projection) { … ProjectionRows … ProjectionMatches … }` block (lines 286-294) to directly after `run.PersistedJobs = …` (line 256), ahead of the completion check. That way a throw, or a tail iteration, always records documents read in the same iteration as the tail and job state.
3. **Optional hardening.** `ProjectionMatches` compares timestamps after millisecond truncation. Every payload in this run fell in the same millisecond, so the Timestamp check was too weak to tell events apart. Consider also asserting `__lastHandledEventSequenceNumber == latest sequence` per key.

## Re-run expectation

With fixes 1 and 2, this run's data would pass the job check. Values would match once the step finished, at about 21:41:25.4Z, roughly 173 s after the clock started. That is about 58 events/s of projection catch-up, which is a throughput finding, not a correctness one.
