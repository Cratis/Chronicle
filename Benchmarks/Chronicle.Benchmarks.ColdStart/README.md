# Client cold-start benchmarks

On-demand (not in the solution, not in the nightly run). Slice 1 of Cratis/Chronicle#5123.

## What it measures

- **Probe** (`probe <scenario>`, run N times by `probe-loop <scenario> <n>`): one fresh process per run, against a warm kernel owned by the host process.
  - P0 runtime start (process start to `Main`), P1 client created, P2 client ready (`GetEventStore`), P3 first `EventLog.Append`; P1-P3 are measured from `Main`.
  - Cold allocations at P2 and P3 (`GC.GetTotalAllocatedBytes(precise: true)`, measured from `Main`).
  - Scenarios: `connect` (auto discovery off, so only the connect) and `auto` (discover and register the shared benchmark artifacts).
  - The loop writes median, p90 and min to `results/probe-<scenario>.json`.
- **`GrpcServiceProxyCreationBenchmark`** (BenchmarkDotNet): cold (30 launches, no warmup) and warm cost of creating proxies: `EagerConnect` (baseline, all 32 as before #5123), `LazyConnect`, `LazyAppendPath` and `LazyAllServices`. No kernel needed. BenchmarkDotNet's Allocated comes from a separate diagnoser run, so the cold job's Allocated is not the first-call allocation; use the probe for that.

## Running

The kernel image is built by `Chronicle.Benchmarks` (its `BuildDocker` target; `CRATIS_CHRONICLE_LOCAL_IMAGE` overrides the tag). Setting `CHRONICLE_COLDSTART_KERNEL` to the connection string of a running kernel skips starting a container.

```shell
dotnet build Benchmarks/Chronicle.Benchmarks.ColdStart -c Release /p:TreatWarningsAsErrors=false /p:DisableDockerBuild=true /p:CratisProxiesOutputPath=
dotnet run -c Release --no-build --project Benchmarks/Chronicle.Benchmarks.ColdStart -- probe-loop connect 20
dotnet run -c Release --no-build --project Benchmarks/Chronicle.Benchmarks.ColdStart -- probe-loop auto 20
dotnet run -c Release --no-build --project Benchmarks/Chronicle.Benchmarks.ColdStart -- --filter '*GrpcServiceProxyCreation*' --exporters json
```

Run on an idle machine; record load average, CPU, RAM, runtime and the kernel image ID with the results.
