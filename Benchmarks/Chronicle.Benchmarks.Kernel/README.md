# Chronicle.Benchmarks.Kernel

In-process BenchmarkDotNet benchmarks for kernel code paths. They call kernel types such as `ProtectedEventTypeMigrations` and `EventGenerationRelease` directly, so they need no Docker, no running kernel and no database. Use them to measure what a change does to allocations and time on hot paths such as append, compliance handling and event delivery. The container-backed benchmarks in `Benchmarks/Chronicle.Benchmarks` measure end-to-end throughput but cannot attribute kernel allocations.

The project uses the in-memory storage providers. It has access to kernel internals through `InternalsVisibleTo` in `Source/Kernel/Core/Core.csproj`.

## Running

Build in Release, then run the produced assembly with a filter:

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net10.0/Chronicle.Benchmarks.Kernel.dll --filter "*AppendGenerationProtectionBenchmark*"
```

The benchmarks use a short in-process job (`Job.ShortRun` with `InProcessEmitToolchain`) and `[MemoryDiagnoser]`. Every report includes an `Allocated` column. Allocations are deterministic, so they are the most reliable signal. Timings vary with machine load.

## Comparing a change with a baseline

1. Build the project on the baseline (for example `origin/main`) and copy `bin/Release/net10.0/` somewhere outside the repository.
2. Build it again with your change and copy that output as well.
3. Run the two copies interleaved (baseline, change, baseline, change) with the same filter, so they see the same machine conditions.
4. Compare `Allocated` first. If the time spreads overlap, treat the timing difference as noise.

## Adding a benchmark

1. Add a class in this project with `[MemoryDiagnoser]` and a `[Benchmark]` method for each case. Prefer one class per hot path. Reuse the configuration pattern in `AppendGenerationProtectionBenchmark` (a nested `ManualConfig` with the short in-process job).
2. Build the kernel objects in `[GlobalSetup]` with the same real collaborators the specs use: the in-memory storage, `JsonSchemaMetadataManager` and the compliance value handlers. Don't use mocks: they allocate and distort the measurement.
3. Return the result from each benchmark method so the JIT can't eliminate the work.
4. Include the common case (for example a single-generation event type or an event without classified properties) next to the case your change targets. Regressions on the common path matter most.

## ReplayChildLookupBenchmark

Measures `Sink.TryFindRootKeyByChildValue` in a MongoDB sink bulk window (the replay and catch-up cache), resolving a root key from a child value across many cached roots that each hold many children. It replays root events, child additions and child updates, and reports time and `Allocated` per scenario, with an `OracleLookup` arm (a plain dictionary) as the upper bound of what any index can save.

The MongoDB collection is a hand-written no-op fake (`NoOpMongoCollection`): bulk writes succeed and a cache miss busy-waits a configurable round trip before returning nothing, so the result does not depend on a server.

```sh
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net10.0/Chronicle.Benchmarks.Kernel.dll --filter "*ReplayChildLookupBenchmark*"
```
