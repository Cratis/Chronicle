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

### Dormant phase

`--observer-footprint --dormant` adds a dormant phase after the subscribed measurement, for [decision 0034](../../decisions/0034-release-dormant-subscribed-observers.md). It sets `observers.dormantAfter` to `--dormant-after`, waits for the kernel to release the dormant observers, and reports three things for the observers that stay subscribed throughout:

1. **released**: the live heap, the retained heap per measured observer, the resident activations of every grain type (the twelve largest), and over a window with nothing appended the grain calls per observer per minute, the bytes allocated per observer per minute and the process CPU.
2. **wake**: the time to wake one observer at a time (median, 95th percentile and maximum over `--wake-samples` observers), then the time to wake all of them in batches of 100. Waking is a call to the observer, which activates it; a kernel that has not released the observer answers from memory.
3. **woken**: the same figures as released, once every observer is resident again.

```sh
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --observer-footprint --dormant --count 2000 --dormant-after 5 --window 30
```

| Option | Default | Meaning |
| --- | --- | --- |
| `--dormant` | off | Runs the dormant phase after the subscribed measurement. |
| `--dormant-after` | 0 | `observers.dormantAfter` in seconds. `0` keeps every subscribed observer resident, which is what every kernel did before the release existed. |
| `--dormant-settle` | twice `--dormant-after` plus 10 s | Seconds to wait for the release before the released measurement. |
| `--window` | 30 | Seconds of each idle window, with nothing appended. |
| `--wake-samples` | 100 | Observers woken one at a time for the latency percentiles. |
| `--watchdog` | 1 | `observers.watchdogInterval` in seconds. The default of the harness is much shorter than the kernel's 60 s, which multiplies every per-minute figure of the window; use `--watchdog 60 --window 125` for the cadence of a real kernel. |

Without a release, released equals subscribed and the wake is a call to a resident grain: that is the baseline. The harness never appends events, so it measures the cost of being dormant and of an activation, not event-triggered wake-up through the appended events queue; that path is measured by the specifications and, in a real kernel, by the wake metrics of decision 0034.

### Lazy observer metric instruments

An observer activation used to create all 17 of its metric instruments up front, each with its own copy of the scope's tags. They are now created the first time the observer records them, and the instruments of a scope share one copy of its tags. The command above, run twice on each build on .NET 11 under the default legacy metric names, measured:

| Build | Subscribed, retained per observer | Unsubscribed, retained per observer |
| --- | --- | --- |
| Before (eager instruments) | 21.9 KiB, 21.8 KiB | 2.8 KiB, 2.8 KiB |
| After (lazy instruments, shared tags) | 18.6 KiB, 18.7 KiB | 3.0 KiB, 2.9 KiB |

That is about 3.2 KiB less for each subscribed observer that records no metrics, about 15 %. The unsubscribed difference is within the run-to-run spread. The machine was under heavy load (load average 28-55), which affects the activation time but not the retained bytes.

## Client subscription keeper calls

The `--keeper-calls` mode measures the grain calls the client subscription keeper makes on an idle kernel. It starts the same in-process silo as `--observer-footprint`, subscribes a number of reactor observers to one client connection, and runs the keeper of every observation stream the way the gRPC reactors service does, with nothing appended. An incoming grain call filter counts the calls to `IsSubscriptionIntact`, `KeepSubscription`, `IsClientTarget` and `KnowsClient`. The output reports them per second and per observer per minute as averages over the whole run, which include the first check of every subscription. It also reports the busiest single second, the steady state over the last `subscriptionCheckIntervalSeconds` of the run, and the process CPU over the run.

The keeper before #5816 checked every subscription every `reviseIntervalSeconds`. To measure that cadence with the current build, set the safety net to the revise interval:

```sh
Cratis__Chronicle__ConnectedClients__SubscriptionCheckIntervalSeconds=2 dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --keeper-calls --count 2000 --seconds 125
```

The CPU figure comes from local grain calls in one process, which skip serialization and the network. Compare it with a `--without-keeper` run of the same length and repeat the runs; differences of a few hundredths of a core are within run-to-run noise.

```sh
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --keeper-calls --count 2000 --seconds 125
```

| Option | Default | Meaning |
| --- | --- | --- |
| `--count` | 1000 | Subscribed reactor observers, each with its own keeper. |
| `--seconds` | 30 | How long to keep them idle. Use more than `connectedClients.subscriptionCheckIntervalSeconds` to see the safety net. |
| `--without-keeper` | off | Runs no keepers, for the CPU baseline of the same observers. |

One run of each with 2 000 observers over 125 s, default options otherwise:

| Run | Keeper calls per second (run average) | Steady state | Busiest second | Process CPU |
| --- | --- | --- | --- | --- |
| Every 2 s (`SubscriptionCheckIntervalSeconds=2`) | 1 983.6 | 2 000 per second | 4 000 | 0.283 cores |
| Default (60 s safety net) | 96.0 | 66.7 per second | 4 000 | 0.297 cores |
| `--without-keeper` | 0 | 0 | 0 | 0.231 cores |

The keeper calls drop about thirtyfold. The busiest second in both runs is the first check of every subscription, one revise interval after it is kept; later safety-net checks are spread over the interval. The CPU difference between the two keeper runs is within run-to-run noise.

## Client streams

The `--client-streams` mode measures what it takes to open many server-streaming calls from one client connection, which is what a client that registers many reactors and reducers does after a restart. It starts a real Kestrel listener on loopback with the kernel's HTTP/2 limits (`KestrelHttp2Limits`) and a probe service whose stream sends one message and then stays open, like an idle observer stream. A client with the channel settings of the Chronicle .NET client opens `--count` of them at once. The output reports how many were established, how long establishment took (the run, and p50, p95 and max per stream), the managed memory the open streams hold in the process (client and server together) and every failure with the gRPC status it carried.

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --client-streams --count 11000
```

| Option | Default | Meaning |
| --- | --- | --- |
| `--count` | 11000 | Observers the client has. |
| `--max-streams` | 100 | The server's concurrent HTTP/2 streams per connection. |
| `--single-connection` | off | The client may not open a second HTTP/2 connection, so streams beyond the server limit queue. |
| `--multiplexed` | off | The observers share one stream, as with the multiplexed observation. |
| `--timeout-seconds` | 120 | How long to wait for every stream to be established. |

The mode isolates the transport. It does not run the kernel's registration, which has its own bound (`observers.maxConcurrentRegistrations`).

## Idle observer calls

The `--idle-calls` mode measures what subscribed observers cost an idle kernel. It starts the same in-process silo as `--observer-footprint`, subscribes a number of reactor observers to one client connection and leaves them alone, with nothing appended. After a settle period it counts every grain call, by interface and method, with an incoming grain call filter, and reports them per observer per minute together with the bytes the process allocated, the gen0 collections and the CPU used in the same window.

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --idle-calls --count 1000 --seconds 30 --watchdog 5
```

| Option | Default | Meaning |
| --- | --- | --- |
| `--count` | 1000 | Subscribed reactor observers. |
| `--seconds` | 30 | How long to measure. |
| `--watchdog` | 5 | `observers.watchdogInterval` in seconds. A shorter interval than the default 60 s fits several ticks into a short run, and multiplies the per-minute figures by the same factor. |
| `--settle` | twice the watchdog interval plus 2 s | Seconds to wait after subscribing before the window starts. |

The `IGrainTimerInvoker.InvokeCallbackAsync` line is the watchdog timer itself, one call per tick. The allocation figure is everything the process allocated in the window, so it includes the harness and the silo's housekeeping, and it is a comparison between builds, not a model of a production kernel.

Two runs of 1 000 observers, 30 s, watchdog interval 5 s, on a machine under heavy load, before and after the shared lookups described below:

| Build | `GetTailSequenceNumber` per observer per minute | `IsConnected` per observer per minute | All grain calls per observer per minute | Allocated per observer per minute |
| --- | --- | --- | --- | --- |
| Before | 12.0, 12.0 | 12.0, 12.0 | 36.2, 36.2 | 251 433 B, 251 965 B |
| After | 3.5, 4.0 | 2.8, 3.1 | 18.5, 19.3 | 136 449 B, 142 255 B |

The watchdog of every observer asks whether its client connection is still registered and what the tail of its event sequence is. Lookups for the same connection, or the same event sequence, that are running at the same moment share one answer; nothing is cached, so no answer is older than one grain call and a disconnect or a new event is noticed on the same tick as before. All the observers of this harness share one connection and one event sequence and tick together, so most of their lookups overlap. Observers in a real kernel tick at different moments, and share fewer lookups.

What the harness does not cover: it is a single silo with in-memory storage, so it has no cross-silo grain calls, no serialization of grain-call payloads and no MongoDB or SQL reads, which is where most of an idle kernel's per-call cost is. A grain call here is local and returns as soon as it is scheduled.

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

## GC settings experiment

`--gc-experiment` runs the multi-namespace GC workload once per GC configuration and prints a comparison table of committed memory after settling, live memory, allocation rate, GC pause total, throughput and the number of `System.Text.Json` options instances the runtime still tracks. Each configuration is a separate child process started with `DOTNET_GC*` environment variables, one setting changed at a time from the kernel's production settings (Server GC, `GCConserveMemory` 5, DATAS on). `--gc-workload` runs a single configuration in the current process, using whatever `DOTNET_GC*` variables are set, and prints one `GCRESULT` JSON line.

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Kernel/Chronicle.Benchmarks.Kernel.csproj -c Release
dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net11.0/Chronicle.Benchmarks.Kernel.dll --gc-experiment --repeats 5 --namespaces 50 --count 2000 --window 60 --settle 60 --hard-limit-mb 6100
```

The workload spreads reactor observers over the namespaces of one event store, then subscribes and unsubscribes all of them repeatedly for the window. It then waits for the settle time without forcing a collection and reads the committed memory, and only afterwards forces full compacting collections to measure the live heap. Use a quiet machine: throughput on a loaded one varies by several times between repeats, and the committed figures depend on how many collections happened.

| Option | Default | Meaning |
| --- | --- | --- |
| `--repeats` | 3 | Runs of each configuration; the table shows the median. |
| `--namespaces` | 20 | Namespaces the observers are spread over. |
| `--count` | 2000 | Observers per cycle. |
| `--window` | 30 | Seconds of subscribe and unsubscribe cycles. |
| `--settle` | 20 | Seconds to wait before reading the committed memory. |
| `--hard-limit-mb` | 0 | Heap hard limit for every configuration. Production runs under about 6.1 GB (`6100`), and Server GC behaves very differently with and without a limit. |
| `--committed-gain` | 20 | Percent lower committed memory a configuration needs to be a candidate. |
| `--regression` | 5 | Percent lower throughput a candidate may have. |
| `--only` | all | Comma-separated configuration names: `baseline`, `heap-count-4`, `conserve-7`, `conserve-9`, `retain-vm-off`, `concurrent-off`, `datas-off`, `workstation`, `hard-limit-75-percent` (without `--hard-limit-mb`). |
| `--affinitize-mask` | none | Adds an `affinitize-mask` configuration with this hexadecimal `GCHeapAffinitizeMask`. |

The `JsonSerializerOptions` column counts the tracked instances after the load window and after the forced collections. A count that grows with the load would point at a path that creates options per call; in the run documented in [Size the kernel for a container](../../Documentation/hosting/container-resources.md) it stayed at 5 across 84,000 subscribe and unsubscribe calls.
