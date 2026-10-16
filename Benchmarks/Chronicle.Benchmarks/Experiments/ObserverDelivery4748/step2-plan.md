I've revised the step-2 plan to address all six major and four minor findings. I agree with every finding. On finding 1, a fully matched transport control would amount to building the engine, so I use a same-engine 1-key control and a capped projection instead (section 7). One thing has moved under me: the step-1 harness is still uncommitted and still being edited. Probe.cs now has a registration-wait loop, and `ValidateProfile` still rejects any transaction. Step 2 therefore starts from whatever step 1 commits and validates.

# Step 2 (revised): benchmark-only pull coordinator for #4748

## Changes from review
1. **(M1)** Pull results are now an optimistic feasibility bound. A same-engine 1-key control splits the gain into "local dispatch" and "no per-key jobs". Registration time is recorded separately for both engines.
2. **(M2)** Two pull modes: checkpoint-only, and capability-equivalent, which also writes per-key per-type handled counts. The write gates use the capability-equivalent totals.
3. **(M3, M4)** The cursor and watermark have explicit "nothing yet" states. The watermark is the completed prefix of one ordered ledger, registered before dispatch. An adversarial self-test proves the saved W never passes a blocked handler.
4. **(M5)** No flush just because the reader is idle. Paced live runs show writes per event against arrival rate. GO requires every gate to pass; every other valid result is investigate or stop.
5. **(M6, minors)** Transaction attribution comes from step 1 unchanged. Added: a selectivity scenario, timing runs with the profiler off, per-process CPU inside the container, and payload checks.

## 1. Where the code lives
- **Prerequisite:** step 1's current harness is still uncommitted and changing (Probe.cs now has a registration wait). Step 2 starts only after step 1 has:
  - passed its smokes, including a real `AppendMany` batch smoke with transaction attribution validated;
  - committed locally.
- **Inheritance:** step 2 takes `ProbeMongo` attribution, `ValidateProfile` and the observer-list rules exactly as step 1 validated them. It does not edit or fork their logic. If step 1 cannot attribute batch transactions, batch rows are INVALID for both engines; no workaround.
- **Branch:** `chronicle-4748-pull`, cut from step 1's commit, with the same kernel SHA `d5d0eea66d` and image `cratis/chronicle:bench-4748`. Output goes to `.ai-work/strong-consistency/pull-<date>/`.
- **Worktree:**
  - Preferred: `git switch -c chronicle-4748-pull` inside `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure`, once step 1 has finished.
  - Otherwise: `git worktree add -b chronicle-4748-pull /Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-pull chronicle-4748-measure`. The first build there needs project references (leave out `-p:BuildProjectReferences=false`, timeout 900).
- **New files in `Benchmarks/Chronicle.Benchmarks/`:**
  - `ProbePull.cs`: `ProbePullCoordinator` (reader, ledger, lanes, flusher) and `ProbePullOptions` (env values echoed into the row).
  - `ProbePullLedger.cs`: ordered completion ledger plus watermark.
  - `ProbePullProgress.cs`: checkpoint and handled-counts writes, and `PullAtTail`.
  - `IPullPageSource`: a Mongo source plus a scripted in-memory source for self-tests.
- **Reused unchanged:**
  - `ProbeDelivery.Record` / `Snapshot` and `ProbeAccounting`;
  - `ProbeRun.Snapshot`;
  - `Probe.Append` / `AddExpected`;
  - `Probe.Run` (deadline, partial and INVALID rules, cleanup, `Save` to `results.jsonl`);
  - `Probe.ValidateProfile`;
  - `ProbeMongo.StartProfile` / `EndProfile` / `State` / `Observers` / `Jobs`.
- **Additive edits** (proposed to step 1 as well, so both engines record the same fields):
  - `Probe.Measure`: a `pull` branch.
  - `CheckObservers`: for pull, exactly the five `SystemObserverIds`.
  - `ProbeRun`: `ProbeObserverId = "probe-4748-pull"`, and new fields `RegisteredMs`, `PayloadMismatches`, `UnrelatedAppended`, `ProfileMode`, `ContainerCpu`, `Pull*`.
  - `Probe.Append`: optional pacing, optional unrelated traffic, and `run.Wake?.Invoke()` after `AddExpected`.
  - `ProbeMongo.attribution`: one extra rule. Comment `'probe-4748-pull'` on `command` or `originatingCommand` (covers getMore) is owned by the probe.
- **Payload verification (minor 4):** `ProbeDelivery.Record` gains an optional `(value, timestamp)` and keeps the first one delivered. `Snapshot` compares it to `run.Appended[position]` and reports `PayloadMismatches`. Any mismatch makes the run INVALID. Step 1's `ProbeReactor` passes `evt.Value` and `evt.Timestamp`; pull decodes them from `content`.

## 2. How events are appended and read
- **Append:** unchanged, through the Chronicle client (`EventLog.Append` / `AppendMany`). Allocation, grain, transactions and `$system.patterns` are all real.
- **No holes from failed appends (agreeing with the reviewer, M3):**
  - The allocator moves forward only after a successful storage append: `Source/Kernel/Core/EventSequences/EventSequence.cs:550–555` (remarks), `:635–639` (`State.SequenceNumber` set after acknowledgement), `:825–831`.
  - So a missing position means delayed visibility, not evidence of a hole. The reader holds and re-reads and never crosses a gap. No failover claim is made.
- **Read target:** database `{store}+es+Default`, collection `event-log`.
  - `_id` is the sequence number, first value `EventSequenceNumber.First = 0` (`Concepts/Events/EventSequenceNumber.cs:15`).
  - Only the primary `_id_` index is used; no new index.
- **Cursor states (M3):** an explicit `ReadCursor { None | At(R) }`, never a sentinel. (Step 1 already tripped over the Decimal128 `Unavailable` sentinel.)
  - `None`: query `{_id:{$gte:0}}`. `At(R)`: query `{_id:{$gt:R}}`.
  - Both: `sort({_id:1}).limit(P).batchSize(P)` with `comment:'probe-4748-pull'`, `P = PULL_PAGE` (default 1000). Fields: `_id`, `eventSourceId`, `type`, `content`; check real names on the first smoke.
  - The expected next position is 0 when `None`, otherwise R+1. If the first document returned is not that position, hold, re-read every 5 ms, and record `gapHolds` / `maxGapHoldMs`. A gap lasting more than 2 s makes the run INVALID.
- **No server-side type filter:** every position goes into the ledger.
  - Non-matching types are marked "not applicable" and completed at once.
  - Matching types (`BenchmarkEvent` `c0b93c8e-…`) are dispatched.
  - Selectivity cost is measured in section 6.

## 3. Ledger, lanes and watermark (M4)
- **Ledger:** a ring indexed by `seq - base`, one entry per position: `state ∈ {Pending, Done}`.
- **Strict order in the single reader task, per page:**
  1. Append the page's entries to the ledger as Pending (not-applicable ones as Done).
  2. Publish R with `Volatile.Write`.
  3. Enqueue the matching events to their lanes.

  Registration before publication guarantees no position ≤ R is ever missing from the ledger.
- **Lanes:** `L = PULL_LANES` lanes (default 64), lane = stable hash of key mod L. Each lane has a bounded channel (capacity 1024) and one sequential consumer, giving per-key order with bounded concurrency. A full lane blocks the reader (backpressure).
- **Handler:** `ProbeDelivery.Current.Record(...)`. Only after it returns, `Volatile.Write(state = Done)`. An entry is never removed until W has passed it.
- **Watermark:** `W` is `None` until position `First` is Done. After that, W = the last position of the contiguous Done prefix.
  - Only the single flusher advances W, and it scans forward from W+1.
  - Every position ≤ R is registered and W never passes a Pending entry, so the saved W is linearizable with respect to handler completion.
- **Removed:** per-lane watermarks. They had no proven meaning.
- **Test hook:** `PULL_DROP_SEQ` (loss self-test only) skips dispatch but marks the entry Done. The checkpoint can then pass the event, which mimics silent loss, and the loss detector must report it.

## 4. Progress document, handled counts and cadence (M2, M5)
- **Checkpoint document:** collection `pull-progress` in `{store}+es+Default`. `_id` is the observer id; fields `w` (null while `None`), `r`, `handled`, `handledPerType`, `revision`, `updatedAt`.
  - Write: `updateOne({_id, revision:n}, {$set…, $inc:{revision:1}})`, upsert on first write, with `w:'majority', j:true` and the coordinator comment. At most one write in flight.
- **Today's extra statistics write:** today also writes per-partition per-event-type handled counts.
  - Done after each handled batch: `Core/Observation/Observer.Handling.cs:349–351` calls `ObserverHandledCountsStorage.Increment`.
  - That is one upsert per partition with `$inc counts.<type>`: `Storage.MongoDB/Observation/ObserverHandledCountsStorage.cs:29–42`, collection `observer-handled-counts`.
- **Two modes, both run:**
  - `PULL_STATS=checkpoint`: the checkpoint only, so per-key statistics are removed. This is an explicit trade-off and is labelled as such.
  - `PULL_STATS=equivalent` (default for the gates): each flush also issues one unordered `bulkWrite` to `pull-handled-counts`. It contains one upsert per key touched since the last flush, using the same `{_id:{observerId,partition}, counts}` shape (copy the BSON from a today run), with `$inc`. Report commands and documents separately.
- **Flush rule:** flush when W has moved and either 1000 completions have built up since the last flush, or `PULL_FLUSH_MS` (default 50) has passed since the first unflushed completion.
  - There is no flush just because the reader is idle. A live lull waits out the timer like any other window.
  - The only extra flush is an explicit `DrainAndStop()` at shutdown. It runs after durable completion has been measured and is excluded from the counts.
- **Durable completion:** `PullAtTail` = `w` is not null and `w ≥ tail`, read with the instrumentation comment in the existing 100 ms poll loop.

## 5. Live, catch-up and registration timing (M1)
- **Catch-up:**
  - Append n events, then start the clock and construct the coordinator (Mongo client, first progress read).
  - Record `RegisteredMs` when the coordinator is ready.
- **Today's catch-up (paired runs):**
  - Already times client construction and registration inside the clock (Probe.cs, catch-up branch after `run.Clock.Start()`).
  - Record `RegisteredMs` there too, when the observer definition appears (step 1's new wait loop).
- **Both engines report:**
  - `evPerSecFull` = n / effect time;
  - `evPerSecPostRegistration` = n / (effect time − `RegisteredMs`).

  The second is a bound only: today's catch-up may start before registration is observed.
- **Live:**
  - Start the coordinator with cursor `None` before warmup. The warmup event is position 0, which tests the M3 edge.
  - `Probe.Warm` waits for `PullAtTail(warmSeq)`.
  - Wake comes from an in-process hint on each append acknowledgement, coalesced, with a fallback poll every 100 ms. Variant `PULL_WAKE=poll` uses polling only.
- **Extra, not comparable to step 1:** catch-up of 10000:100 with 1000 live appends running at the same time (the #4583 shape). It must show zero loss.

## 6. Scenarios (same strings, observer field `pull`)
- **1:1 with step 1:**
  - catch-up 2000:2000:64, 10000:10000:64 and 10000:100:64;
  - live 1000:1:1, 1000:1:16, 1000:1000:16 and 1000:1:1:batch;
  - append-only twins (rerun this session);
  - the `:noop` variants of rows 1 and 3;
  - 3 repeats each of rows 1, 3 and `live:…:1:16`.
- **Not mapped:** `catchup:projection` (N/A).
- **Matched controls (M1), both engines:**
  - `catchup:{reactor,pull}:10000:1:64`: one key, so today's per-key job overhead is spread over 10,000 events. This gives today's ceiling for transport plus Orleans plus serialization.
  - `PULL_LANES=1` on rows 1 and 3.
  - Paired today runs of rows 1, 2, 3 and the 1-key control, adjacent to the pull runs.
- **Paced live (M5), both engines:** `live:*:300:1:1:rate=20`, `rate=200`, and `live:*:1000:1:16:rate=1000`. Pacing is an additive `Probe.Append` option. Report writes per event against rate.
- **Selectivity (minor 1), both engines:** `catchup:*:2000:2000:64:sparse9`.
  - Nine `BenchmarkInstanceRecorded` events per relevant event, interleaved on the same keys. They are recorded as `UnrelatedAppended` and excluded from expected membership. Any delivery of them is "unexpected".
  - If this pair is not run, conclusions are restricted to "dense subscription".
- **Profiling off (minor 2):**
  - `PROBE_PROFILE=off` reruns of rows 1, 2, 3, the 1-key control and `live:…:1:16`, both engines.
  - These are timing-only rows (status `COMPLETE_TIMING_ONLY`). Membership and payload checks still apply; Mongo cost is null and not required.
  - The decisive ev/s and lag figures come from these rows, with the profiled rows supplying cost.
- **CPU (minor 3):**
  - `ContainerCpu` is the combined cgroup `cpu.stat`, labelled "Chronicle kernel + mongod combined". The fixture runs mongod in the same container (`ChronicleBenchmarkFixture.cs:113–114`, `Storage__ConnectionDetails` pointing at `localhost:27017`).
  - Per-process utime and stime come from `/proc/<pid>/stat` for the server and `mongod`, read by `ExecAsync` at clock start and end.
  - Client CPU is reported separately; in pull mode it includes the engine.

## 7. Success and stop criteria (relative to fresh paired medians, profiling off for timing)
- **Correctness gate, every run:**
  - missing = 0, unexpected = 0, duplicates = 0, order violations = 0, `PayloadMismatches` = 0;
  - no gap crossed;
  - final `w == tail`;
  - observer list contains exactly the five system observers;
  - non-zero probe-owned operations in profiled rows.
- **Catch-up, projected:**
  - For each thin-key row, take the lower of pull's ev/s and today's ev/s on the 1-key control (the transport ceiling).
  - At 2000 keys the projected figure must be at least 10x today's, comparing post-registration with post-registration and full with full.
  - At 10000 keys, pull must finish and the projected figure must be at least 2,000 ev/s, or the 1-key ceiling if that is lower (then reported as ceiling-bound).
  - Key-count insensitivity: pull ev/s at 10000 keys at least 50% of pull at 100 keys.
  - The 100-key row must be at least 1x today.
  - With `sparse9`, pull must be at least 3x today's relevant-event rate.
- **Writes (equivalent mode, commands per event):**
  - catch-up ≤ 0.02;
  - paced at ≥ 200 ev/s and the unpaced 1:16 row: ≤ 0.2;
  - paced at 20 ev/s: ≤ 2, versus about 6 today;
  - documents written per event are reported for both engines.
- **Reads:** coordinator reads per live event ≤ 1.0 at `1:1:1` and ≤ 0.3 at `:16`.
- **Total live observer cost:** pull minus twin, all operations per event, ≤ 30% of today's.
- **Lag:**
  - durable gap ≤ `FLUSH_MS` + 150 ms;
  - live drain lag (effect − append) no worse than today's, or ≤ 100 ms.
- **Decision:**
  - **GO** (continue to step 3; not "make default") only if every gate passes.
  - **STOP** if:
    - a structural correctness failure is not explained by a harness bug, or
    - projected catch-up at 2000 keys is under 5x today's, or
    - equivalent-mode live writes are at or above today's.
  - **INVESTIGATE** for every other valid result. Maximum 2 days, then GO or STOP.
  - INVALID runs mean fix the harness; they never count toward the verdict.
  - If lanes=64 is no faster than lanes=1 and client CPU is near a full core, the result is client-bound.

## 8. What this deliberately does not prove
- Engine-level throughput: the result is a feasibility bound.
- Crash and restart safety, or the duplicate bound after a crash.
- Behaviour under failover or rollback, and durable-write enforcement.
- Ownership, fencing, duplicate activations, attempt ids, upgrades.
- Failure isolation, parking and retry, and how healthy lanes behave under saturation.
- Multi-silo behaviour, many observers sharing a reader, cache fill.
- Reducer or projection semantics, and completion/read-your-writes.

## 9. Risks
- **Optimistic bias remains:** the 1-key ceiling assumes transport cost does not grow with key count.
- **Step-1 dependency:** batch-transaction attribution may stay unresolved, leaving batch rows INVALID.
- **Shape mismatches:** the handled-counts `_id` shape and event field names must be copied from real documents.
- **Paced-run effects:** pacing changes today's results too, so only paired runs are compared.
- **Drift and overhead:** shared-machine drift (paired runs and repeats; no concurrent heavy phases); profiler overhead (hence timing-only rows).
- **Optimistic wake:** the append-acknowledgement hint may be better than a real kernel wake (hence the poll variant).
- **Self-test realism:** the self-test scripts the races in-process; Mongo timing may differ.

## 10. Implementation checklist (one implementer, about 7 working days)
1. **Day 0 – preconditions:**
   - Step 1's batch smoke validated and its harness committed.
   - Branch and SHA checked.
   - `lsof -nP -iTCP:27448 -sTCP:LISTEN` (and 35448) shows nothing.
   - `df -h ~` shows at least 40 GiB free.
2. **Day 1 – code and build:**
   - Ledger, coordinator, progress and stats, additive harness fields, the attribution comment rule, payload capture.
   - Build:
     ```
     ~/.pi/agent/bin/pi-phase run --kind build --label '4748 pull build' --timeout 240 --queue-timeout 300 --log <out>/build.log --tail 4 -- dotnet build Benchmarks/Chronicle.Benchmarks/Chronicle.Benchmarks.csproj -c Debug --no-restore -p:BuildProjectReferences=false -p:DisableDockerBuild=true -p:CratisProxiesOutputPath=
     ```
3. **Day 2 – `probe pull-self-test`** (`pi-phase run --kind test … probe pull-self-test`) on the scripted source. Cases:
   - (a) a one-event log at position 0: W stays `None` until it is Done;
   - (b) a blocked handler on key K at seq s, with 10k later completions and a 1 ms flusher: every saved W < s, then W reaches the tail after release;
   - (c) randomized delays: at every write, all positions ≤ W are in an independent completion set;
   - (d) a delayed-visibility gap is held and never crossed;
   - (e) per-key order across lanes;
   - (f) `PULL_DROP_SEQ` is reported missing.
4. **Day 3 – smokes, one phase each:**
   - Command pattern:
     ```
     CRATIS_CHRONICLE_LOCAL_IMAGE=cratis/chronicle:bench-4748 BENCH_MONGO_PORT=27448 BENCH_CHRONICLE_PORT=35448 PROBE_OUT=<out> PULL_LANES=64 PULL_PAGE=1000 PULL_FLUSH_MS=50 PULL_STATS=equivalent ~/.pi/agent/bin/pi-phase run --kind integration --label '4748 pull smoke live' --timeout 240 --queue-timeout 300 --log <out>/smoke-live.log --tail 0 -- dotnet Benchmarks/Chronicle.Benchmarks/bin/Debug/net10.0/Chronicle.Benchmarks.dll probe live:pull:10:1:1
     ```
   - Then `catchup:pull:100:10:4`, `live:pull:100:1:1:batch`, and a reactor smoke with payload checks.
   - Inspect profiler samples, the progress and counts documents, and the observer list.
5. **Day 3 – live drop run:** `PULL_DROP_SEQ` must report exactly that position missing and be INVALID.
6. **Days 4–5 – profiled matrix (section 6):**
   - Long catch-up runs: `--timeout 480 --queue-timeout 60` with `PROBE_DEADLINE_SECONDS=420`.
   - Retry exit 75 (admission) at most 6 times. Stop after two unchanged substantive failures.
7. **Day 6 – the rest:** timing-only rows (`PROBE_PROFILE=off`), paced, sparse and controls, repeats, paired today runs.
8. **Day 7 – write-up:** `RESULTS-pull.md` with:
   - a side-by-side table: full and post-registration ev/s, projected ev/s, checkpoint and equivalent writes (commands and documents), reads, correctness and payload, effect vs durable, client and container CPU split;
   - the verdict;
   - local commit only, and a copy of the harness to `<out>/harness/`.

### Critical Files for Implementation
- `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure/Benchmarks/Chronicle.Benchmarks/Probe.cs`: Measure/Append/Warm hooks, registration timing, the inherited INVALID and transaction rules.
- `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure/Benchmarks/Chronicle.Benchmarks/ProbeMongo.cs`: attribution inherited from step 1, plus the coordinator comment and getMore rule.
- `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure/Benchmarks/Chronicle.Benchmarks/ProbeDelivery.cs`: exact accounting, plus additive payload verification.
- `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure/Source/Kernel/Storage.MongoDB/Observation/ObserverHandledCountsStorage.cs`: today's handled-counts write shape, which the capability-equivalent mode reproduces.
- `/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure/Source/Kernel/Storage.MongoDB/EventSequences/EventSequenceStorage.cs`: the `event-log` `_id`, index and query shapes the reader mirrors.
