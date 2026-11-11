# Chronicle.Benchmarks.Kernel

In-process BenchmarkDotNet benchmarks for kernel code paths. They call kernel types such as `ProtectedEventTypeMigrations` and `EventGenerationRelease` directly, so they need no Docker, no running kernel and no database. Use them to measure what a change does to allocations and time on hot paths such as append, compliance handling and event delivery. The container-backed benchmarks in `Benchmarks/Chronicle.Benchmarks` measure end-to-end throughput but cannot attribute kernel allocations.

The project uses the in-memory storage providers. It has access to kernel internals through `InternalsVisibleTo` in `Source/Kernel/Core/Core.csproj`.

## Running

Build in Release, then run the produced assembly with a filter:

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --filter "*AppendGenerationProtectionBenchmark*"
```

The benchmarks use a short in-process job (`Job.ShortRun` with `InProcessEmitToolchain`) and `[MemoryDiagnoser]`. Every report includes an `Allocated` column. Allocations are deterministic, so they are the most reliable signal. Timings vary with machine load.

## Observer activation footprint

BenchmarkDotNet measures what a call allocates, not what stays resident between calls. The `--observer-footprint` mode measures what one observer grain activation keeps on the live heap. It starts an Orleans silo with Chronicle and in-memory storage in the process, subscribes a number of reactor observers, forces full compacting collections and reports the growth of the live heap divided by the number of observers. It then unsubscribes them, waits for the idle release (see `observers.idleDeactivationAfter`) and measures again.

All observers are served by one client connection that the harness registers with the silo, so the watchdog keeps their subscriptions. The warm-up observers stay subscribed for the whole run, so they are part of the baseline and of both measurements. The output reports how many measured observers were subscribed when the subscribed phase was measured, and how many warm-up observers were still subscribed at the end. The harness stops with an error if it can no longer remove the kernel's startup task, or if an option is not a whole number of zero or more.

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --observer-footprint --count 2000 --idle-deactivation 3 --collection-age 10
```

| Option | Default | Meaning |
| --- | --- | --- |
| `--count` | 2000 | Observers to activate after a warm-up of a tenth of that (at most 200). |
| `--idle-deactivation` | 0 | `observers.idleDeactivationAfter` in seconds. `0` keeps every observer resident. |
| `--collection-age` | Orleans default | Orleans' grain collection age in seconds, so grains without `[KeepAlive]`, such as the `ObserverAlerts` companion of each observer, are collected within the run. The harness waits for the warm-up observers' companions to be collected before it takes the baseline. |
| `--settle` | 120 s plus twice the longest period when idle release is on | Seconds to wait after unsubscribing. The first alert reminder tick, spread over the first two minutes, has to give the reminder up before an observer counts as idle. |
| `--hold`, `--hold-unsubscribed` | 0 | Seconds to pause after the subscribed or the unsubscribed measurement, for `dotnet-gcdump collect -p <pid>`. |

Compare runs with different `--count` values, or `dotnet-gcdump report` output from two runs, to attribute the growth to types. Results from November 2026 are in [decision 0030](../../decisions/0030-release-idle-unsubscribed-observers.md).

### Lazy observer metric instruments

An observer activation used to create all 17 of its metric instruments up front, each with its own copy of the scope's tags. They are now created the first time the observer records them, and the instruments of a scope share one copy of its tags. The command above, run twice on each build on .NET 11 under the default legacy metric names, measured:

| Build | Subscribed, retained per observer | Unsubscribed, retained per observer |
| --- | --- | --- |
| Before (eager instruments) | 21.9 KiB, 21.8 KiB | 2.8 KiB, 2.8 KiB |
| After (lazy instruments, shared tags) | 18.6 KiB, 18.7 KiB | 3.0 KiB, 2.9 KiB |

That is about 3.2 KiB less for each subscribed observer that records no metrics, about 15 %. The unsubscribed difference is within the run-to-run spread. The machine was under heavy load (load average 28-55), which affects the activation time but not the retained bytes.

## Comparing a change with a baseline

1. Build the project on the baseline (for example `origin/main`) and copy `bin/Release/net11.0/` somewhere outside the repository.
2. Build it again with your change and copy that output as well.
3. Run the two copies interleaved (baseline, change, baseline, change) with the same filter, so they see the same machine conditions.
4. Compare `Allocated` first. If the time spreads overlap, treat the timing difference as noise.

## Adding a benchmark

1. Add a class in this project with `[MemoryDiagnoser]` and a `[Benchmark]` method for each case. Prefer one class per hot path. Reuse the configuration pattern in `AppendGenerationProtectionBenchmark` (a nested `ManualConfig` with the short in-process job).
2. Build the kernel objects in `[GlobalSetup]` with the same real collaborators the specs use: the in-memory storage, `JsonSchemaMetadataManager` and the compliance value handlers. Don't use mocks: they allocate and distort the measurement.
3. Return the result from each benchmark method so the JIT can't eliminate the work.
4. Include the common case (for example a single-generation event type or an event without classified properties) next to the case your change targets. Regressions on the common path matter most.
