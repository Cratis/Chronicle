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
2. Run all benchmarks (TestContainers will automatically start Chronicle and MongoDB)
3. Generate results in `results/` directory
4. Clean up infrastructure automatically

## Infrastructure

The benchmarks use **TestContainers** to automatically manage the Chronicle infrastructure:
- Chronicle Kernel container is built from the local source using `cratis/chronicle:local-development` image
- MongoDB container is automatically started and linked
- All containers are cleaned up automatically after benchmarks complete

This approach matches the Integration test strategy used in the `Integration/Api` project.

## Benchmarks

### AppendBenchmark

Measures the performance of appending single events to the event log.

### AppendManyBenchmark

Measures the performance of bulk appending events to the event log.

Parameters:
- EventCount: 10, 100, 1000 events per batch

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

## Results

Benchmark results are exported in JSON format compatible with BenchmarkDotNet and GitHub Actions benchmark visualization.

Results are published to the `Documentation/benchmarks/` directory and visualized at:
https://cratis.github.io/Chronicle/benchmarks/

## CI/CD

Benchmarks are automatically run on push to `main` branch via the `.github/workflows/benchmarks.yml` workflow.
