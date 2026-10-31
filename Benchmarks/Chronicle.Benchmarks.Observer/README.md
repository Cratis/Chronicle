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

The observer fixture was ported from `5b8eb6b402`. Optional outstanding-job and
sequence-scoped state-save APIs are configured through reflection during setup
only, so this same source also builds against the fixed MAIN revision that
predates those APIs. Reflection is not part of the measured operation.

## PII schema metadata (in process)

`SchemaMetadataPaths` measures the real kernel `JsonSchemaMetadataManager` and
`PIICompliancePropertyValueHandler`, including encryption/decryption. Key storage
and provisioning are substituted; their allocations are included. The parsed
schema uses the metadata produced by `[PII]` on a scalar property and a protected
leaf inside a `$ref` nested type. Parsing, key generation, and preparation of the
ciphertext for release are outside measurement. Setup checks that both leaves
were encrypted and that the complete document round-trips. Client-side schema
generation, external key storage, and networking are not measured.

Use the invocation/unroll settings above for this class too. For the observer
comparison, `--filter '*ObserverPaths*BatchSize: 1)'` selects both paths at one
event per batch. For PII, use `--filter '*SchemaMetadataPaths*'`. Interleave MAIN,
TRAIN4, MAIN, TRAIN4; retain each run's JSON report, load, and scheduling snapshot.
