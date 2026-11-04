# Client startup and scenario setup benchmarks

This runner requires no server, Docker, or network service. It measures the real
client/testing implementation through a project reference, not a stand-in cache.

The runner covers both 128 and 1,024 distinct event types. Half of each catalog
has classic reducers with distinct read models; half has non-generic self-reducing models. Reflection.Emit creates the
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
- `FreshReadModelMaterialization`: create a fresh scenario, fold a real event,
  verify the resulting count, then dispose it.
- `FreshReactorDelivery`: create a fresh scenario with a run-local service,
  verify the real handler received one event, then dispose it.
- `SharedStoreScenarioSetup`: construct and dispose a borrowed scenario over
  the prepared store. On revisions before `CreateReadModelScenario` existed,
  use the fresh-scenario API instead. This is intentionally an API-strategy
  comparison, not identical work. Reflection selects and binds the factory
  once outside timing; it is not part of the measured work.

Caches are warmed before BenchmarkDotNet measurement. These workloads do **not**
measure cold first-process startup, append transport, or automatic kernel observer
fan-out; reactor delivery explicitly exercises the production invoker in-process. ShortRun's three samples can have wide confidence intervals
on a shared machine; report that uncertainty rather than treating lower means
alone as proof of a latency improvement. Runtime dependencies explicitly listed
in this runner match those `Testing` embeds when packed, because private kernel
assets do not flow through a project reference.

## Fresh-process scenario and retention proof

After building, run the same executable with `--scenario-proof <output-file>`
instead of BenchmarkDotNet arguments. It uses the 1,024-event catalog, excludes
Reflection.Emit fixture generation from timing, records cold-root setup, then
first/median/p95 timings, allocations and post-GC retained bytes for 30 complete
runs of each scenario operation. Read-model and reactor cases verify the work
actually happened. A final warmed 100-run reactor case records two 50-run retained
heap deltas and weak references to completed scenarios. It exits nonzero if a
scenario survives collection or total retained growth exceeds 5 MiB.

```bash
dotnet Benchmarks/Chronicle.Benchmarks.ClientStartup/bin/Release/net10.0/Chronicle.Benchmarks.ClientStartup.dll \
  --scenario-proof .ai-work/client-startup-proof.txt
```

Repeat this command in at least ten fresh processes on each revision, using the
same harness source and package dependencies. Do not compare an undisposed
baseline with a disposed head run. The JSON/schema and registration phases are
not individually instrumented here; phase-level diagnosis remains a separate,
local-only measurement if an end-to-end budget fails.
