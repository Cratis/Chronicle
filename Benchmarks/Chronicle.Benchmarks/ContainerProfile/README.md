# Container hosting profile

The opt-in `--container-profile` mode is separate from BenchmarkDotNet. It measures a
kernel container under explicit CPU, memory and swap limits while the client and
MongoDB remain unconstrained. It does not change the production Dockerfile or runtime
configuration. Build without triggering the existing benchmark Docker target:

```bash
dotnet build Benchmarks/Chronicle.Benchmarks/Chronicle.Benchmarks.csproj -c Release -m:2 -nodeReuse:false -p:DisableDockerBuild=true
```

Preview the exact shared command builders without starting **any** child process:

```bash
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile plan --matrix core
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile plan --matrix full --verbose --gc-trace
```

`core` has 40 cells: 36 ReadyToRun resource/GC combinations, plus JIT comparisons for
Server and Workstation GC at 1 GiB and both CPU quotas. `full` has 72 cells. Both use
1/2 CPUs, 512 MiB/1 GiB/2 GiB, and six GC variants: shipped Server GC, explicit DATAS
on/off, ConserveMemory=0, heap hard-limit=75%, and Workstation GC. Defaults are three
untraced repetitions, three stores, five namespaces each, 200 seed and 30,000 measured
events per pair, batches of 100, and four concurrent appenders. Override these with
`--stores`, `--namespaces`, `--seed-events`, `--events`, `--batch-size`, `--appenders`,
`--repetitions`, or comma-separated substring filters in `--cells`.

The following commands **build images or start containers**; run them only on a quiet
host with explicit permission. All output defaults to the ignored
`Benchmarks/results/container-profile/<timestamp>/`; `--results <dir>` overrides it.

```bash
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile build-images --variants r2r,jit
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile r2r-check --image chronicle-container-profile:<shortsha>-r2r
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile run --matrix core --gc-trace
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile cleanup
```

Images are self-contained Linux publishes for Docker's architecture, built with the
unchanged production Dockerfile and copy script in a scratch build context. Workbench
static assets are absent (an empty directory satisfies the copy script). Compare kernel
results only; these are not Workbench cold-start measurements. Chronicle PE headers are
checked before building; `r2r-check` copies files from a stopped container and checks
them again, without starting the kernel. Image ID and uncompressed size are recorded.
MongoDB defaults to `mongo:8.2`; `CHRONICLE_MONGODB_IMAGE` can select another image.

Each repetition uses a new MongoDB replica set and kernel, ephemeral credentials and
certificate, loopback-published ports, and ownership/session labels. Readiness is the
first HTTP 200 from `/health/ready` within 180 seconds after Docker start returns;
`warmPageCache=true` is explicit because OS page caches are not cleared. After 30 idle
seconds, PID 1 RSS/HWM and Docker cgroup memory/CPU are sampled separately. Append-only
throughput and first-append-to-distinct-observer-delivery time have separate fields.
Sampling uses a labelled BusyBox PID-namespace helper and can perturb the host.

Per-run JSON and aggregate JSON/CSV preserve missing values as null/empty, unsuccessful
rows and OOM/exit state. CSV summaries use successful **untraced** repetitions only and
show counts plus medians/ranges. `--gc-trace` adds one separately labelled repetition
per cell; its EventPipe file is parsed with TraceEvent for GC generations, effective
heap counts and nearest-rank pause percentiles. Gen2 collects the LOH; no separate LOH
count is fabricated. Truncated/OOM traces are not parsed. Drain time is null because
there is no kernel drain marker; `docker stop --time 60` wall time is reported instead.

The one-minute host-load ceiling defaults to half the client CPU count. The harness
waits up to five minutes before recording a skipped row. `--max-host-load` changes the
ceiling; `--allow-noisy-host` explicitly permits, and records, noisy runs. Child commands
have deadlines and their process trees are killed on timeout. Cleanup is exact-target
or label-scoped, never daemon-wide; it does not remove result files or scratch images.
Do not run `cleanup` while another container-profile session is active. No production
performance claim follows from a plan, a build, or a noisy-host run.
