# Client startup and scenario setup benchmarks

This runner requires no server, Docker, or network service. It measures the real
client/testing implementation through a project reference, not a stand-in cache.

The catalog contains 128 distinct event types, 64 classic reducers with distinct
read models, and 64 non-generic self-reducing models. Reflection.Emit creates the
catalog once, outside timing. Setup verifies event registration and an actual
successful reducer dispatch before any measurements are accepted.

## Run

From the repository root:

```bash
dotnet build Benchmarks/Chronicle.Benchmarks.ClientStartup -c Release -warnaserror
dotnet run --project Benchmarks/Chronicle.Benchmarks.ClientStartup -c Release --no-build -- \
  --filter '*ClientStartupBenchmark*' --artifacts .ai-work/client-startup
```

The runner uses BenchmarkDotNet's ShortRun job, in-process emit toolchain and
MemoryDiagnoser. It returns a nonzero exit code if any benchmark fails or no
benchmarks are selected. The in-process toolchain avoids a second generated
project build and keeps comparison runs small. Compare separate processes on
the same machine and preserve Mean, Error and Allocated, not just ratios.

## Workloads

- `DiscoverAndSeed`: construct and dispose a new `EventStoreForTesting` for an
  equivalent, already warmed application catalog. This includes real reducer
  discovery, event-type/schema seeding and store setup, not just reflection.
- `InvokeOneEvent`: invoke one classic `ReducerInvoker` with one real event,
  including activation and folding. The invoker and input array are prepared
  outside timing; each invocation starts with no prior model state.
- `FreshScenarioSetup`: construct and dispose a `ReadModelScenario`, forcing
  `ReadModels` so its lazy event-store preparation is included.
- `SharedStoreScenarioSetup`: construct and dispose a borrowed scenario over
  the prepared store. On revisions before `CreateReadModelScenario` existed,
  use the fresh-scenario API instead. This is intentionally an API-strategy
  comparison, not identical work. Reflection selects and binds the factory
  once outside timing; it is not part of the measured work.

Caches are warmed before measurement. These workloads do **not** measure cold
first-process startup, append transport, event serialization, or end-to-end
observer delivery. ShortRun's three samples can have wide confidence intervals
on a shared machine; report that uncertainty rather than treating lower means
alone as proof of a latency improvement. Runtime dependencies explicitly listed
in this runner match those `Testing` embeds when packed, because private kernel
assets do not flow through a project reference.
