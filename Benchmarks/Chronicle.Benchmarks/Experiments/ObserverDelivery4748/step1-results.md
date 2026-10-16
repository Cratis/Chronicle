# Step 1 final report — observer delivery baseline (Cratis/Chronicle#4748)

Engine `d5d0eea66d`, image `cratis/chronicle:bench-4748`, unchanged. Harness: worktree `.worktrees/chronicle-4748-measure`, branch `chronicle-4748-measure`, final commit `aebfded2bd` (local, not pushed). `results.jsonl` has 61 rows; rows 1–19 are early smokes, 20–40 the validation and matrix pass, and 41–61 the latest harness generations. Every figure below was recomputed from `results.jsonl`. The earlier report is kept as `RESULTS-superseded-pre-final.md`.

Notation: `kind:observer:events:keys:concurrency[:batch]`. "ev/s" uses the original clock, started before registration. "post-reg" starts at observer registration. "Ops" are Mongo commands seen by the profiler, per expected event. "Durable" is persisted observer tail plus job completion. R = reactor.

## 1. Headline table

All valid rows have exact membership: missing, duplicates, first-delivery order violations and unexpected were 0. "Gap" is effect→durable. Medians are over valid COMPLETE samples; the range follows in brackets.

| Scenario | Status (rows) | ev/s | post-reg ev/s | Ops/event | Observer-only ops/event (live−twin) | Effect → durable |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| catchup R 200:200 | COMPLETE (41; 20 earlier) | 187.5 | 293.6 | 11.77 | n/a | gap 1.2 s |
| **catchup R 2000:2000** | 3 COMPLETE (34, 49, 50), 3 failed job (42 DNF, 53, 54 JOB_FAILED) | **351.9** [204.3–428.0] | 393.1 [225.9–471.3] | 12.01 [11.97–12.06] | n/a | effect 5.7 s [4.7–9.8]; durable 21.5 s [17.9–35.2]; **gap 16.8 s [12.2–25.4]** |
| catchup R 10000:100 | 2 COMPLETE (57, 58); 55 INVALID; 56 DNF (harness gap, data valid) | 14,589 [14,504–14,675] | 32,200 [30,884–33,524] | 0.126 [0.1255–0.1264] | n/a | durable ≈1.0 s; gap 0.29–0.33 s |
| **catchup R 10000:10000** | DID_NOT_FINISH (60), one sample | 372.9 (effects only) | 382.6 | 8.59 (incomplete) | n/a | durable never reached: job stalled (#4954) |
| catchup projection 10000:100 | COMPLETE (61); 59 INVALID (harness bugs) | 83.0 | 83.6 | 3.04 | n/a | effect 120.5 s; gap 0.15 s |
| live R 1000:1:1 | COMPLETE (30; twin 29) | 33.7 | n/a | 4.87 | 1.54 | gap 0.12 s |
| live R 1000:1:16 | 3 COMPLETE (26, 31, 32; twin 25) | 302.1 [72.5–333.3] | n/a | 2.73 [2.65–2.73] | 0.75 [0.67–0.76] | gap 0.11 s [0.106–0.116] |
| live R 1000:1000:16 | COMPLETE (28; twin 27) | 43.6 | n/a | 7.08 | 2.99 | gap 0.05 s |
| live R 1000:1:1 batch | COMPLETE (24; twin 23) | 2,714 | n/a | 0.031 | 0.023 | gap 0.05 s |
| append-only twins | COMPLETE (23, 25, 27, 29) | 5,764 / 176.6 / 79.7 / 52.1 (batch, 1:16, 1000:1000:16, 1:1) | n/a | 0.008 / 1.98 / 4.09 / 3.34 | n/a | n/a |

Notes on the table:
- The 2k median uses three samples taken with three different harness generations. Row 34 predates the system-owner fix. Row 50 had kernel debug logging on. Only the headline rates and totals are compared. They are not a controlled repeat set.
- The 10k/100 median uses only rows 57 and 58, so it is two samples. The 14,675 and 14,504 in the brief are the original-clock rates. The post-reg rates are about 31–34k ev/s.
- Row 56 delivered 10000/10000 exactly and reached durable in 1.8 s. It was marked DNF only because the harness's system-durable check demanded an empty patterns in-flight list. That was fixed in `c3592907ca` and `7fa5eb4497`. I keep it out of the repeat statistics.
- The live medians are the three 1:16 rows only. Each twin is a single sample. The four twin rows are "append only" (a probe-less reactor store), so live−twin includes run-to-run noise.
- Projection loss, duplicates and order are not measured per event. Row 61 verifies the final sink state only: 100 of 100 documents match the latest event per key.
- Catch-up has no twin, so observer-only ops are n/a. The catch-up ops figures include job machinery.

## 2. Owner split (rows with the fixed classifier)

The classifier labels an operation "probe" if exactly one known observer id appears in it and it is the probe's. "system" means one other known id. "shared" means no observer id or an ambiguous one (event-log reads, job-step reads and removes, sink writes). Labels do not change the totals. Rows before 41 have system = 0 because of the `$literal` bug, so I do not split them.

| Row | Probe | System | Shared |
| --- | ---: | ---: | ---: |
| 41 R 200:200 | 1434 | 66 | 853 |
| 49 R 2000:2000 | 13893 | 64 | 10056 |
| 50 R 2000:2000 | 13818 | 64 | 10053 |
| 57 R 10000:100 | 734 | 64 | 466 |
| 58 R 10000:100 | 734 | 66 | 455 |
| 61 projection 10000:100 | 20149 | 68 | 10167 |
| 60 R 10000:10000 (DNF) | 52833 | 80 | 32966 |

- System work inside the measured window is a constant of about 64–80 operations (mostly `observers` updates), independent of event count. System-observer cost per live event shows up in the twin rows, not here.
- The exact classification of `command.q._id: "$system.patterns"` updates is still not replayed. `classifier-replay.py` never ran, and no patterns updates fall inside the post-drain profiles. Do not cite it as verified.

## 3. What today's engine writes per event (per-collection picture)

D = `<store>+es+Default`, in operations per expected event.

**Catch-up reactor, one key per event (2000:2000, rows 49/50):** about 12.0 ops per event, which is about 12 ops per partition (job step).

| Collection / op | Ops/event |
| --- | ---: |
| D.job-steps update | 3.89 |
| D.jobs update | 2.00 |
| D.observers update | 1.02 |
| D.observer-handled-counts update | 1.00 |
| D.event-log query | 1.00 |
| D.job-steps query | 1.00 |
| D.job-steps remove | 1.00 |
| D.failed-job-steps remove | 1.00 |
| everything else (registration, event-types, reactors) | about 0.1 |

**Catch-up reactor, 100 keys (rows 57/58):** the same per-partition cost spread over 100 events per key. That is 300 job-step updates, 204 jobs updates, about 112–131 observer updates, 100 handled-count updates and about 100 each of job-step queries and removes, for 1255–1264 ops in total. **Catch-up writes scale with the number of keys, not events.** The 100-event run is 0.126 ops per event, a 95x reduction against 12.0.

**Catch-up projection, 100 keys (row 61):** one step for the whole job, but per-event writes: 1.003 observer-state updates, 1.000 handled-count updates and 1.000 sink update, plus 0.01 queries. That is 3.04 ops per event, and it is serial (83 ev/s, about 12 ms per event).

**Live reactor:**
- The append alone costs 1.98–4.09 ops per event. That is one event-log insert plus the system `$system.patterns` observer. The patterns observer writes about 0.64–2.0 observer updates and 0.32–1.0 handled-count updates per event (the twin rows 25, 27, 29), plus `behavior-patterns` updates.
- The probe observer adds 0.67–0.76 at 1:16, 1.54 at 1:1, and 2.99 at 1000 keys: roughly 1 observer-state update and 0.5–1 handled-count update per delivered batch or key.
- Batch append is the exception: 0.031 total, and 0.023 above its twin.

Terminal transaction commands are NOT PROFILED. Batch transaction counts were checked against `serverStatus` instead. Phase separation (registration versus delivery) is approximate.

## 4. Findings

### (a) #4954 — catch-up job bookkeeping fails under per-partition load

Mechanism, from `diagnosis-failed-catchup.md`: every catch-up step makes a non-interleaved `Observer.ReportHandledEvents` call (a lock plus 2 Mongo writes) on one grain. The calls queue serially. When the queue exceeds the Orleans 30 s response timeout, the tail steps time out after the client has already handled the event. The step fails, there is no `PartitionFailed`, `failedPartitionCount` stays 0, `handledEventCount` is undercounted, and the job is retained. **In every occurrence the client received every event exactly once.**

| Row | Setting | Client | Job outcome |
| --- | --- | --- | --- |
| 42 | 2k/2k, default timeout | 2000/2000, effect 8.2 s | status 6, 38 failed steps (1962 ok); observer runningState 4 |
| 51 | 2k/2k, forced 5 s timeout | 2000/2000 | stuck at status 4 until deadline, steps orphaned Running |
| 52 | 2k/2k, forced 5 s timeout | 2000/2000 | same: 396 ok / 535 failed / 1092 Running (per the handoff) |
| 53 | 2k/2k, default timeout | 2000/2000, effect 12.7 s | status 6 at 46.2 s, **293 failed steps** |
| 54 | 2k/2k, default timeout | 2000/2000, effect 10.9 s | status 6 at 42.1 s, **32 failed steps** |
| 60 | 10k/10k, default timeout | 10000/10000, effect 26.8 s | never completed. After 600 s, progress was 1145 successful / 4529 failed of 10000, the rest Running. The job ended status 7 (stopped) at 8 min, probably at harness teardown (unverified). The `failed-job-steps` collection holds 7320 documents, more than the 4529 progress counter. |
| 60 (also) | `$system.patterns` own catch-up job, in the same row | n/a | **status 6, 2821 of 5395 steps failed**, so system observers are hit too |

- Rows 34, 49 and 50 (2k/2k, default timeout) completed, with a gap of 25.4 s, 12.2 s and 16.8 s. Even successful runs spend 12–25 s between the last effect and job completion, which is the queue draining. Row 34's step window was 29.6 s, row 42's 35.0 s.
- At the default timeout, the 2k/2k outcome is 3 successes and 3 failures out of 6, and the single 10k/10k run failed.
- Rows 51 and 52 are forced reproductions and are not throughput samples. Row 50 (debug logging on) completed, so debug logging is not a known trigger.
- The trigger is the number of partitions (steps), not the event count. 10,000 events on 100 keys complete in about 1 s with no failures (rows 57 and 58).
- Not established: the exact text of the failed steps' exceptions at the default timeout. Retained exception arrays are empty. The row 42 `runningState 4` may be a read after client disposal.

### (b) `$system.patterns` in-flight anomaly

Evidence from the observer state recorded at tail and at the end of each 10k row:

| Row | Scenario | handledEventCount / 10000 | in-flight partitions | Shortfall ÷ in-flight |
| --- | --- | ---: | ---: | ---: |
| 55 | R 10000:100 (INVALID, drain deadline) | 7932 | 15 | 138 |
| 56 | R 10000:100 | 8368 | 18 | 91 |
| 57 | R 10000:100 | 8327 | 17 | 98 |
| 58 | R 10000:100 | 8515 | 18 | 83 |
| 59 | projection 10000:100 | 8169 | 16 | 114 |
| 61 | projection 10000:100 | 8387 | 17 | 95 |
| 60 | R 10000:10000 | 7226 | 0 | n/a |

- In every row the observer reports `lastHandled 9999`, `next 10000`, `runningState 1`, `failedPartitionCount 0` and empty catching-up and replaying lists. The in-flight ids are all keys of the probe's 100 keys (15/15, 18/18 and so on), so they are real partitions, not noise.
- **The state is stable.** In row 61 the persisted state about two minutes after tail is identical to the state at tail: 8387 handled and 17 in-flight.
- The anomaly appears in 10k rows only. Rows 34–54 (200 and 2000 events) show handled = 2000 and no in-flight partitions, both initially and when connected.
- The shortfall ÷ in-flight ratio is roughly 83–138, close to the 100 events per key. That fits "each in-flight partition holds its last unreported batch". **This is a hypothesis, not a finding.**
- Row 60 has no in-flight partitions yet is still 2774 short. Its patterns job failed (see (a)), so it may be the #4954 mechanism. The 10k/100 rows have no failed patterns job in the evidence, so for them the mechanism is unexplained.
- It could be an engine defect (partition progress is not completed or reported for the system observer) or an interaction with seeding 10k events at concurrency 64 while patterns is live. Not diagnosed. The kernel logs are `*-kernel.log` for rows 55–61 (row 60's is 106 MB).
- Effect on the baseline: the probe observer's own counts are correct in the rows where its job completed. It does mean `handledEventCount` is not a reliable count of delivered events for this system observer. The harness now records `COMPLETE_WITH_INFLIGHT` when this is the only blocker (`c3592907ca`, `7fa5eb4497`). The measured window is unaffected because the profile starts after the drain. Row 55 (INVALID setup-deadline) is the case where the drain waited 300 s for in-flight to clear.

### (c) Key-count sensitivity, reactor versus projection

| Keys (10000 events unless noted) | Reactor ev/s | Ops/event | Outcome |
| --- | ---: | ---: | --- |
| 100 | 14,589 median (31k post-reg) | 0.126 | completes in about 1 s, no failures |
| 2000 (2000 events) | 352 median | 12.0 | completes in 3 of 6 default-timeout runs |
| 10000 | 373 | 8.6 incomplete | job stalls |

- The throughput drop tracks the key count, not the event count: 41x from 100 to 2000 keys. The per-partition job-step cost (about 12 operations) is paid once per key.
- With 100 keys the reactor is 175x faster than the projection (14.6k versus 83 ev/s), because the projection pays 3 serial writes per event and the reactor pays per key.
- The projection has no key-count scaling problem in the data we have. It runs as a single step with no failures. It is slow because of three writes per event on one serial path. At 2000 or 10000 keys the projection was not run. Its 83 ev/s is for 100 keys only.
- The projection's job completes after a single step. The old completion check needed a final write the kernel never makes; see `diagnosis-projection-10k.md`. The fix is in `aebfded2bd`.

## 5. What step 1 answers for step 2

Step 2's ordered reader (the pull coordinator, per `../step2-plan-r2.md`) has to beat these numbers. The plan's gates are relative to fresh paired medians, so rerun today's engine next to it, but these are the starting points.

- **Catch-up at 2000 keys:** today 352 ev/s (post-reg 393) with 12 ops/event. GO needs at least 10x (about 3,500–3,900 ev/s). Under 5x (about 1,800–2,000 ev/s) is STOP. Today also fails the job half of the time, which pull should avoid.
- **Catch-up at 10000 keys:** today does not finish (373 ev/s of effects, no durable). Pull has to finish and reach at least 2,000 ev/s, or the 1-key ceiling if lower. The 1-key control (`catchup:*:10000:1:64`) was **not run** in step 1.
- **Catch-up at 100 keys:** today 14.6k ev/s at 0.126 ops/event. Pull must be at least 1x. It cannot beat today by much, and it has to keep pull's own 10k-key rate at least 50% of its 100-key rate.
- **Catch-up writes:** the gate is at most 0.02 commands/event. Today is 12.0 at 2000 or 10000 keys and 0.126 at 100 keys. Today's 100-key figure is already within 6x of the gate, so only the thin-key rows give a real margin.
- **Live cost:** pull minus twin must be at most 30% of today's observer-only cost: at most about 0.23 ops/event at 1:16 (today 0.75), 0.46 at 1:1 (today 1.54), 0.9 at 1000 keys (today 2.99). Batch live is already tiny (0.023).
- **Durable lag:** gate is `FLUSH_MS` + 150 ms. Today's live gap is 50–120 ms, so pull must keep up with that. Catch-up gap today is 12–25 s at 2k keys and 0.3 s at 100 keys.
- **Projection:** step 2 does not cover it. Today's 83 ev/s at 100 keys is the only reference.
- **Capability-equivalent mode:** today's patterns observer undercounts `handledEventCount` at 10k events. Do not compare exact counts against today's. Compare against the event log.
- **Not collected in step 1, and needed by the plan:** the 1-key control and `PULL_LANES=1` rows; profile-off timing rows (all rows here are profiled, so ev/s carry profiling overhead); paced live rows; `sparse9`; the `:noop` rows (removed by ruling); three repeats of 10k/10k (one DNF); live append during catch-up.

## 6. Uncertainty

- **Shared machine.** Other workloads ran concurrently (admission queues, up to 6 attempts per build or run). This is probably one reason 2k/2k varies from 204 to 428 ev/s.
- **Debug build, profiling on.** The Mongo profiler plus Debug binaries perturb absolute rates. Ops/event is unaffected, ev/s is not. No profiling-off run exists.
- **Single or few samples.** 10k/10k, projection 10k/100 and every twin are single samples. 10k/100 reactor has two. Only live 1:16 and 2k/2k have three. Ranges are not confidence intervals.
- **Mixed harness generations.** Rows span `cbd51850a6` through `aebfded2bd`. Row 34 predates the owner fix. Row 50 had debug logging. Rows 51 and 52 had a 5 s timeout.
- **Failure rate of #4954** (3 of 6 at 2k) is load-dependent and host-dependent, so treat it as "can fail", not as a rate.
- **Effect and durable are client observations**, not crash or failover proofs. Quiescence is 2 s. Delivery order is first-delivery order only.
- **Patterns anomaly** is unexplained. Its in-flight hypothesis is not tested.
- **Ownership** of `$system.patterns` updates is not replay-verified; the 10k/100 observer-only ops are not separable from system work except through the `system` label.
- **Live twin subtraction** includes system-observer cost that varies by run.
- Build gate: local harness exempt from Tier 1; analyzer warnings were waived locally (about 150–190); CodeScene unavailable.
