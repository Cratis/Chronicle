# Chronicle Benchmarks

This directory contains performance benchmarks for Chronicle.

There are two suites:

- **`Chronicle.Benchmarks`** — single node, against the Chronicle container. Run in CI, results published.
- **`Chronicle.Benchmarks.Clustering`** — the same kinds of workload measured at one silo and at two, to
  answer whether clustering helps. Run on demand, see [below](#clustering-benchmarks).

## Running Benchmarks

### Prerequisites

- Docker (for building and running Chronicle container)
- .NET 10 SDK

### Execute

```bash
./run.sh
```

This will:
1. Build the benchmark project (which also builds the Chronicle Docker image)
2. Run all benchmarks (TestContainers automatically starts the Chronicle container, which embeds MongoDB)
3. Generate results in `results/` directory
4. Clean up infrastructure automatically

## Infrastructure

The benchmarks use **TestContainers** to automatically manage the Chronicle infrastructure:
- Chronicle Kernel container is built from the local source using `cratis/chronicle:local-development` image
- MongoDB runs inside that same container; its port is published alongside Chronicle's
- All containers are cleaned up automatically after benchmarks complete

This approach matches the Integration test strategy used in the `Integration/Api` project.

## Benchmarks

### AppendBenchmark

Measures the performance of appending single events to the event log.

### AppendManyBenchmark

Measures the performance of bulk appending events to the event log.

Parameters:
- EventCount: 10, 100, 1000 events per batch

### StronglyConsistentPerKeyReadBenchmark

Measures a strongly consistent read of one key as its stream grows, for a passive projection (folded in the Kernel
by an immediate projection grain) and a passive reducer (folded in the client).

Parameters:
- StreamLength: 10, 100, 1000, 10000 events for the key

The projection is read cold (a fresh session, so a newly activated grain folds the whole stream), warm (nothing
appended since the last read), warm after one event appended to the key, and warm after one event appended to another
key. The passive reducer has no cache, so it is read once per iteration.

### Future Benchmarks

The following observer benchmarks are planned but require additional infrastructure:

- **ReducerBenchmark**: Measure events processed per second by reducers
- **ReactorBenchmark**: Measure events processed per second by reactors  
- **ProjectionBenchmark**: Measure events processed per second by projections

These will be implemented once we have:
1. A way to deploy observers to the Chronicle server
2. Mechanisms to measure observer processing throughput
3. Integration with the in-process Chronicle client for easier testing

## Clustering Benchmarks

`Chronicle.Benchmarks.Clustering` measures the same workload against three cluster shapes and reports them
side by side, so any difference is attributable to clustering rather than to the workload:

| Topology | Shape |
|---|---|
| `SingleSilo` | One silo hosting every grain type |
| `TwoSilos` | Two silos, both allowed to host every grain type |
| `TwoSilosWithSplitRoles` | Two silos, event sequences on one and observers on the other, so every event crosses the boundary |

The harness is the in-process Orleans test cluster over EphemeralMongo, configured exactly like
`Integration/Clustering/ClusteringFixture` with the silo count and role assignment parameterized. No Docker
is involved.

| Benchmark | Measured window |
|---|---|
| `ClusteredAppendBenchmark` | 100 sequential single appends of an event type no observer subscribes to |
| `ClusteredAppendManyBenchmark` | 10 concurrent batches of 50, same event type |
| `ClusteredProjectionBenchmark` | Appending 500 events across 20 event sources **and** the projection's observer reporting it handled up to that tail |
| `ClusteredReplayBenchmark` | Replaying a 2000 event corpus (seeded in setup) until the observer is caught up again and every job it spawned has finished |

Every mean is reported per event. Observer waits poll every 50 ms, which is the granularity floor of the
projection and replay results.

Both observer benchmarks refuse to run unless their observer is actually subscribed — an unsubscribed
observer would let the window close on the append alone and quietly report a meaningless number.

```bash
dotnet run -c Release --project Chronicle.Benchmarks.Clustering -- --filter '*'
```

Silos share one machine, so two silos will not show ideal scaling. The question these answer is whether
clustering helps, is neutral, or costs — not whether it scales linearly.

## Rebuild under live load (MongoDB)

The clustering suite also has a standalone, single-silo diagnostic scenario (not a BenchmarkDotNet
microbenchmark). It uses the existing EphemeralMongo replica set and in-process kernel; no Docker is
required. It runs once, validates the seeded and rebuilt data, prints one JSON measurement object,
and disposes the cluster. Normal suite filters are unchanged.

From the repository root:

```bash
dotnet run -c Release --project Benchmarks/Chronicle.Benchmarks.Clustering \
  -p:CratisProxiesOutputPath= -- --rebuild-live
```

| Environment variable | Default | Meaning |
|---|---:|---|
| `REBUILD_EVENTS` | 10000 | Historical value updates, plus one marker seed and one catalog event |
| `REBUILD_LIVE_RATE` | 10 | Target marker appends/second |
| `REBUILD_SAMPLE_MS` | 100 | Minimum pause between samples |
| `REBUILD_TIMEOUT_SECONDS` | 240 | Deadline covering seed and measurement (not cluster startup) |

Historical updates cycle across 40 keys. A separate live marker key shares their catalog join, so
this exercises `From` **and** `Join`, not just a single-From copy. Key cardinality stays bounded even
with a large event corpus; the list probe reads all 41 models in one page and selects the marker.
Both before and after replay, every historical key's last revision and joined value are checked.
The run fails on failed partitions, an incomplete dataset, deadline expiry, or no measured live/rebuild
overlap. For a faster smoke run, lower `REBUILD_EVENTS`; if it becomes too fast to sample, increase it.

For a large local run (not CI), allow sufficient seed time; seeding uses sequential acknowledged appends:

```bash
REBUILD_EVENTS=1000000 REBUILD_LIVE_RATE=20 REBUILD_TIMEOUT_SECONDS=7200 \
  dotnet run -c Release --no-build --project Benchmarks/Chronicle.Benchmarks.Clustering -- --rebuild-live
```

### Measurement boundaries

- `RebuildThroughSwapSeconds`: replay request through the **observed served collection UUID change**.
  This includes replay preparation and MongoDB promotion; it is not pure fold CPU time. The UUID probe
  detects the swap independently of observer catch-up, avoiding the old folded replay-plus-catch-up number.
- `PostSwapCatchUpSeconds`: observed swap through both read paths seeing the frozen live acknowledgement
  fence. The producer stops at the observed swap; an in-flight append is acknowledged before freezing.
  `Phases` contains named timestamps so later promote/rollback work can add boundaries to this runner.
- `HistoricalEventsPerSecond`: historical corpus size divided by request-to-swap time. Replay may also
  process live events, so this is a conservative historical-prefix rate, not total folded events/second.
- `ServedList`: materialized list read of the served container. `KernelKeyLookup`: client
  `GetInstanceById`, which reaches the kernel materialized store's `FindByKey` path. Each samples against
  acknowledgements captured **before** its read. Reports include revision lag, oldest missing acknowledgement
  age, missing/regressing reads, and acknowledgement-to-first-observed-visibility latency. Full timestamped
  samples carry phase labels; the two paths are not assumed to have identical freshness during replay.
- The sampling interval plus read/UUID request latency bounds temporal resolution. Visibility latencies
  are observed upper bounds, not exact processing latency. The producer uses a periodic timer and sequential
  acknowledged appends: overloaded ticks coalesce rather than generating unbounded concurrent load.
  Compare requested and observed rates; the latter is reported rather than claiming the target was sustained.
- Working set is the maximum sampled during measurement; allocation bytes are a process-wide GC counter
  delta. The kernel, client, probes and Orleans share the process, so these are **not kernel-only** values.
  They exclude setup/seed but include measurement overhead; MongoDB memory is not included.

The diagnostic JSON is separate from the BenchmarkDotNet JSON consumed by the CI benchmark publisher.
Remaining baseline coverage includes SQL, three silos, million-event CI datasets, more projection shapes,
comparative historical runs, and generation-specific promote/rollback timing; this increment adds none of them.
There is no benchmark specs project. These scenario-local helpers are verified by the bounded real-kernel
smoke run and its non-empty overlap/data checks, not by introducing a new specs infrastructure.

## Interleaved nested replay (throughput shape)

Use `--replay-shape` for the throughput-shape series in [#5156](https://github.com/Cratis/Chronicle/issues/5156).
It runs a single in-process silo over ephemeral MongoDB, seeds an acknowledged event log, waits for
projection catch-up, then measures one full replay. It does not access an external environment.

```bash
dotnet build Benchmarks/Chronicle.Benchmarks.Clustering -c Release -p:CratisProxiesOutputPath=
REPLAY_EVENTS=20000 dotnet run -c Release --no-build \
  --project Benchmarks/Chronicle.Benchmarks.Clustering -p:CratisProxiesOutputPath= -- --replay-shape
```

| Environment variable | Default | Meaning |
|---|---:|---|
| `REPLAY_ROOTS` | 2000 | Number of root documents, each with its own event source |
| `REPLAY_DEPTH` | 2 | Child levels: **1 or 2 only**; larger depths are rejected |
| `REPLAY_EVENTS` | 50000 | Exact total workload events, including creation, updates and removal |
| `REPLAY_RUN_LENGTH` | 1 | Maximum consecutive updates from one child source before rotating |
| `REPLAY_RUN_LENGTH_MIX` | unset | Seeded maximum-length:weight buckets; overrides `REPLAY_RUN_LENGTH` when set |
| `REPLAY_REMOVE_PERCENT` | 10 | Percentage of leaf children removed, rounded up; 0–100 |
| `REPLAY_TIMEOUT_SECONDS` | 1200 | Deadline covering startup, seed, replay and content verification |

The fixed shuffle seed is 5261. Every root has one first-level child, and at depth 2 that child has
one leaf. Each child has its own event source. `ChildrenFrom` resolves the first child's root key,
and the leaf's parent-child key through the parent hierarchy. Explicit `Children` and `Leaves`
collections avoid recursive model definitions and duplicate Mongo array-filter names. Renames use
`SetFrom`; leaf removals use `RemovedWith`. All surviving child names and identities are checked.

Creation is ordered by level so parents exist first; sources are shuffled within each level.
Updates rotate across shuffled child sources in runs of at most `REPLAY_RUN_LENGTH`; the final
run may be shorter. Removal comes last. With run length 1, adjacent updates have different sources.
With `REPLAY_RUN_LENGTH_MIX`, bucket weights are shares of **runs**, not shares of events; they need
not sum to 100. Maxima must be strictly increasing positive integers, with finite positive weights.
A bucket spans the previous maximum plus one through its own maximum (the first starts at 1).
The seeded generator picks a bucket by weight, then samples a length uniformly inside it. Mixed
updates rotate through child sources using a global event budget, reserving at least one rename per
child; budget exhaustion can shorten runs. Fixed-length mode retains its even per-source update budget.
Creation and removal still come in separate phases, so this approximates run shape rather than the
production distribution of events per source or the fraction of child events.

```bash
REPLAY_EVENTS=50000 REPLAY_ROOTS=2750 REPLAY_DEPTH=2 \
  REPLAY_RUN_LENGTH_MIX='1:45,2:6.5,4:6.1,8:24.4,16:12.5,64:5' \
  REPLAY_TIMEOUT_SECONDS=2400 dotnet run -c Release --no-build \
  --project Benchmarks/Chronicle.Benchmarks.Clustering -- --replay-shape
```

`RunLengthHistogram` in the JSON maps exact realized consecutive-source lengths to run counts over
**all** workload events, including creation/removal and shortened or adjacent merged runs. The sum
of `length × count` equals `EventCount`; normalize counts by their sum for shares of runs. It is not
an assertion that the realized distribution matches the requested update-only mix.

Appends use acknowledged batches of 500, outside the measured window. The event budget must cover
root and child creation, at least one rename per child, and the configured removals; invalid settings
fail before starting the cluster. The fixture's warm-up event is not a workload/replayed event.

Seed projection catch-up can dominate total runtime for many roots. Progress goes to stderr; one
measurement object is printed to stdout, alongside any framework console logs. For a longer local
run, keep the roots/depth fixed and allow sufficient **total** runtime:

```bash
REPLAY_EVENTS=300000 REPLAY_TIMEOUT_SECONDS=14400 dotnet run -c Release --no-build \
  --project Benchmarks/Chronicle.Benchmarks.Clustering -- --replay-shape
```

### Replay measurement boundaries

- `ReplaySeconds` starts immediately before the replay request and ends after its job completes or
  disappears, the observer is Active at the event-log tail, and no jobs are in flight. It includes
  replay preparation, generation promotion, catch-up and readiness probes, not just fold CPU time.
- `EventsPerSecond` divides the configured workload count by that window. `DocumentsWritten` is the
  verified final root-document count, **not** the number of physical Mongo writes or nested children.
- `MongoCommands` counts driver `CommandStartedEvent`s from Chronicle's `IMongoDBClientManager`
  clients only. It excludes Orleans persistence/membership and Arc clients. It includes reads,
  writes, getMore, readiness probes, background work and commands which subsequently fail.
- `GrainCalls` counts all incoming silo grain invocations in the same window, including readiness
  probes and background work; calls bypassing Orleans dispatch are excluded. Neither metric is
  projection-only. The JSON includes both totals, per-workload-event ratios and boundary descriptions.
- Checkpoint-cadence fields (#5265), all counted in the same window:

  | Field | Meaning |
  |---|---|
  | `SinkBulks`, `SinkBulkOpsMean`, `SinkBulkOpsMax` | Sink write bulks and operations per bulk. Insert, update and delete commands on read-model collections are grouped by driver `OperationId`, and array lengths are summed |
  | `SinkBulksAtOrAbove1000` | Bulks that reached the sink's 1,000-operation limit |
  | `SinkBulkOpsHistogram` | Bulk counts by operation count: `1`, `2-99`, `100-499`, `500-999`, `1000+` |
  | `FlushForCalls`, `FlushForCallsPer1000Events` | Checkpoint fan-outs (`IObserverService.FlushFor`) in total and per 1,000 workload events |
  | `EventCursorPages` | `getMore` commands on the event log plus one; how many cursor pages the readers consumed |

- Seeding and content verification are outside the counter window. Both snapshots independently
  check expected root/child counts, identities, last names and removals, then compare sorted-content
  SHA-256 hashes. A missing, incorrect or changed document fails the run instead of emitting a result.

These are diagnostic measurements, not BenchmarkDotNet publisher input. The default deadline may
need raising on slower machines, particularly at 300k events. Keep the machine idle during replay
and compare the same workload settings and counter boundaries across revisions.

## Results

Benchmark results are exported in JSON format compatible with BenchmarkDotNet and GitHub Actions benchmark visualization.

Results are published to the `Documentation/benchmarks/` directory and visualized at:
https://cratis.github.io/Chronicle/benchmarks/

## CI/CD

Benchmarks run nightly and on manual dispatch via `.github/workflows/benchmarks.yml`, not on every push to `main`.
