# Observer paths (in process)

BenchmarkDotNet with MemoryDiagnoser; no MongoDB, Docker, or running silo.
The real observer is activated by Orleans TestKit. Persistence, queues, jobs,
and subscriber boundaries are substitutes. Results are per batch (1 or 100 events).
Assertions reject skipped live delivery or incomplete catch-up coordination.

- `LiveHandling`: `Observer.Handle`, including subscriber delivery and progress writes.
- `CatchUpRound`: catch-up request, partition registration, handled-count report,
  and completion/handover. This measures observer-side coordination, **not** the
  catch-up job's event reader/subscriber throughput.

The live cursor is reset outside measurement to reuse preallocated input.
Substitute invocation records are cleared outside measurement between iterations.
Iteration hooks require invocation count and unroll factor 1. Storage I/O,
serialization and network costs are deliberately excluded; substitute allocations
are included. Short single-invocation timing results on a loaded host are noisy.

```sh
dotnet build Benchmarks/Chronicle.Benchmarks.Observer/Chronicle.Benchmarks.Observer.csproj \
  -c Release -warnaserror -p:DisableProxyGenerator=true \
  -p:DisableProtoGeneration=true -p:DisableGrpcContractGeneration=true \
  -p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false \
  -p:PublishRepositoryUrl=false

dotnet Benchmarks/Chronicle.Benchmarks.Observer/bin/Release/net10.0/Chronicle.Benchmarks.Observer.dll \
  --filter '*' --inProcess --job short --warmupCount 3 --iterationCount 10 \
  --invocationCount 1 --unrollFactor 1 --artifacts <output-directory>
```

Use the same benchmark sources and build properties for every comparison tree.
