---
title: Size the kernel for a container
description: Memory, CPU and garbage collection settings for the Chronicle kernel image, small-container and throughput profiles measured under Docker limits, how to size a container from your own peak with a measured example, and how to choose between a shared kernel and a kernel per tenant.
---

This page describes how the Chronicle kernel image runs today, which .NET garbage collection (GC) settings you can change, and how to size a container limit from your own workload. For the general production checklist, see [Production deployment](production.md). For single-instance platforms, see [Container Apps, App Service and Cloud Run](azure-container-hosting.md).

## What the image runs with

The production image is built with these settings:

- **Server GC.** The kernel project sets `ServerGarbageCollection` to `true`.
- **Conserve memory.** The runtime configuration sets `System.GC.ConserveMemory` to `5`. A higher value makes the GC trade more frequent collections, and possibly longer pauses, for a smaller heap.
- **Self-contained ReadyToRun publish.** The release workflow publishes `linux-x64` and `linux-arm64` with `PublishReadyToRun=true` and `--self-contained`, so the published assemblies carry precompiled code and the kernel does less just-in-time compilation during startup.
- **Base image.** `mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled-extra`. The kernel is self-contained, so the image needs only the native dependencies, including ICU and time zone data, and no shared .NET runtime. The image runs `./Cratis.Chronicle.Server` directly as the entry point.
- **Host shutdown timeout.** The host waits 10 seconds for a graceful stop.

The kernel does not set any other GC option, such as `GCHeapHardLimit`, `GCDynamicAdaptationMode` or `GCHeapCount`. Since .NET 9, the runtime turns on dynamic adaptation to application sizes (DATAS) by default whenever Server GC is used, so the image runs Server GC with DATAS.

## How the runtime reads container limits

When the process runs in a container with a memory limit, the .NET GC treats that limit as the machine's physical memory. If you do not set a heap hard limit, the default in that situation is the larger of 20 MB and 75 % of the container limit. See [Heap hard limit](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector#heap-hard-limit) and [Heap hard limit percent](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector#heap-hard-limit-percent) in the .NET documentation.

## GC settings you can change

Set a GC option as an environment variable on the container, or as a `configProperties` entry in the runtime configuration. Environment variable values for `DOTNET_GC*` options that take numbers are hexadecimal, while the runtime configuration takes decimal values. For example, `DOTNET_GCHeapHardLimitPercent=0x4B` is 75 %.

| Environment variable | Runtime configuration name | Values | Effect |
| --- | --- | --- | --- |
| `DOTNET_gcServer` | `System.GC.Server` | `1` server, `0` workstation | Selects server or workstation GC. Workstation GC is the .NET default. |
| `DOTNET_GCConserveMemory` | `System.GC.ConserveMemory` | `0` to `9` | Higher values keep the heap smaller, with more frequent collections and possibly longer pauses. |
| `DOTNET_GCHeapHardLimit` | `System.GC.HeapHardLimit` | Bytes, hexadecimal in the environment | Caps the commit size of the GC heap and its bookkeeping. |
| `DOTNET_GCHeapHardLimitPercent` | `System.GC.HeapHardLimitPercent` | Percentage, hexadecimal in the environment | Caps the heap as a percentage of the container limit. Ignored when `GCHeapHardLimit` is set. |
| `DOTNET_GCDynamicAdaptationMode` | `System.GC.DynamicAdaptationMode` | `1` enabled, `0` disabled | Turns DATAS on or off. It only applies to Server GC, where it is on by default. DATAS starts with one heap, adds heaps as the workload needs them and aims to keep the heap roughly proportional to the long-lived data size. `0` gives classic Server GC, with one heap per CPU the container may use. |

Each option is described in [Runtime configuration options for garbage collection](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector).

Run the image with a memory limit and a heap cap:

```bash
docker run --memory 2g -e DOTNET_GCHeapHardLimitPercent=0x4B <image>
```

Set the same variables in Docker Compose:

```yaml
services:
  chronicle:
    mem_limit: 2g
    environment:
      DOTNET_GCHeapHardLimitPercent: "0x4B"
```

Replace `<image>` with the Chronicle production image and tag you deploy, and replace the example limits with values from your own workload.

## Choose a container profile

The image defaults suit a kernel that serves real traffic. When you run Chronicle in a small container, or want the most throughput from a larger one, start from one of these two profiles. They follow from the [measured profiles](#measured-profiles) below, but the memory limit in either profile must still come from your own peak, as described in [Size from your own peak](#size-from-your-own-peak).

| | Small container | Throughput |
| --- | --- | --- |
| Fits | Development and test environments, a kernel per tenant, low event rates, cost-bound hosting | A shared kernel with many namespaces and observers, sustained appends, catch-up after deployments |
| CPU | One CPU | Two or more CPUs. The step from one CPU to two gave the largest gain |
| GC | Keep the image default, Server GC with DATAS. At one CPU the GC mode made no measurable difference | Keep the image default, Server GC with DATAS |
| Memory limit | Above your observed peak. In the measurements, 512 MiB ran out of memory and 1 GiB left about 25 % headroom | Above your observed peak, with headroom for cold start |
| Heap cap | Leave the default of 75 % of the limit, or lower it with `DOTNET_GCHeapHardLimitPercent` when the container runs other processes | Leave the default |

Two settings are trades rather than profiles:

- In the measurements, **Workstation GC** (`DOTNET_gcServer=0`) lowered peak memory by about a quarter with two or more CPUs, and cost 30 to 43 % of the append throughput. Use it only when memory, not CPU, limits a container with several CPUs.
- **Classic Server GC** (`DOTNET_GCDynamicAdaptationMode=0`, DATAS off) raised peak memory by 27 to 41 % and did not raise throughput, so keep DATAS on. Pause times were not measured.

### Small container

Give the kernel one CPU and keep the image's GC settings:

```bash
docker run --cpus 1 --memory 1g <image>
```

With a CPU limit of one, the runtime sees a single processor and runs a single-heap GC whichever mode is configured. The measurements bear this out: Server GC, Workstation GC and Server GC without DATAS were within a few percent of each other on every figure, so switching the GC mode buys nothing at one CPU. The image keeps `System.GC.ConserveMemory` at `5`, which keeps the heap small in every mode.

A single CPU is the expensive part of this profile: the kernel became ready about 1.4 seconds later than with two CPUs, appended at under a third of the rate and needed more than three times as long to bring its observers up to date.

### Throughput

Give the kernel two or more CPUs and keep the image default, Server GC with DATAS:

```bash
docker run --cpus 4 --memory 4g <image>
```

Server GC lets each CPU allocate into its own heap and collects on one thread per heap, and DATAS, on by default, adds heaps as the workload needs them, so an idle kernel does not hold a heap per core. In the measurements, going from one CPU to two raised append throughput 3.4 times and cut observer catch-up from 169 to 49 seconds; going from two to four raised throughput by another 37 % and cut catch-up to 36 seconds.

Avoid Workstation GC on a kernel that has several cores and a busy workload: its single heap does on one thread the collection work Server GC spreads across cores, which is where its throughput loss comes from.

### Measured profiles

Each figure is the median of three runs, with the range in parentheses. Memory is the container's cgroup usage as `docker stats` reports it, which is what the container limit is measured against. The kernel process's resident set was about 210 to 235 MiB higher in every cell, because it also counts file-backed pages, such as the mapped assemblies, that `docker stats` leaves out.

| CPU and memory limit | GC | Ready (s) | Idle memory (MiB) | Peak memory (MiB) | Appends (events/s) | Observer catch-up (s) |
| --- | --- | --- | --- | --- | --- | --- |
| 1 CPU, 1 GiB | Server GC with DATAS (default) | 7.6 (7.3–7.6) | 335 (334–367) | 757 (733–762) | 2,668 (2,647–2,771) | 169 (162–170) |
| 1 CPU, 1 GiB | Workstation GC | 7.5 (7.4–7.5) | 329 (325–341) | 732 (727–746) | 2,711 (2,677–2,722) | 166 (165–168) |
| 1 CPU, 1 GiB | Server GC, DATAS off | 7.4 (7.4–7.8) | 319 (319–322) | 729 (720–732) | 2,640 (2,600–2,712) | 171 (166–173) |
| 2 CPUs, 2 GiB | Server GC with DATAS (default) | 6.2 (6.1–6.2) | 424 (418–428) | 1,037 (1,030–1,083) | 9,161 (8,786–9,240) | 49 (49–51) |
| 2 CPUs, 2 GiB | Workstation GC | 6.3 (6.1–6.3) | 344 (341–346) | 780 (773–783) | 6,382 (5,687–6,456) | 71 (70–79) |
| 2 CPUs, 2 GiB | Server GC, DATAS off | 6.1 (6.0–6.1) | 483 (467–484) | 1,316 (1,270–1,345) | 8,986 (8,099–9,209) | 50 (49–56) |
| 4 CPUs, 4 GiB | Server GC with DATAS (default) | 5.9 (5.9–6.2) | 439 (429–443) | 1,143 (1,107–1,175) | 12,525 (12,494–12,553) | 36 (36–36) |
| 4 CPUs, 4 GiB | Workstation GC | 6.2 (5.9–6.5) | 357 (348–359) | 827 (816–840) | 7,097 (6,698–7,182) | 64 (63–67) |
| 4 CPUs, 4 GiB | Server GC, DATAS off | 5.9 (5.9–5.9) | 519 (499–540) | 1,607 (1,558–1,727) | 12,619 (12,021–12,732) | 36 (35–38) |

No run in the table ran out of memory. One confirmation run with 1 CPU, 512 MiB and the default GC became ready in 7.4 seconds and was killed for running out of memory during the workload, at 497 MiB.

How the figures were taken:

- **Date and version.** 11 November 2026, with the kernel source of Chronicle 19.155.0, published self-contained with ReadyToRun for `linux-arm64` and built into the production image the way the harness builds it.
- **Host.** Docker Desktop 29.8.0 on an Apple silicon Mac, with a Docker VM of 8 CPUs and 16 GiB. Every run started with a one-minute host load below 6 on 14 logical CPUs, and runs that spanned a host sleep were discarded and repeated.
- **Limits.** Docker `--cpus` and `--memory`, with `--memory-swap` equal to the memory limit, on the kernel container only. MongoDB 8.2 and the workload client ran without limits.
- **Workload.** The harness default: 3 event stores with 5 namespaces each, 200 seed events and 30,000 measured events per namespace, appended in batches of 100 by 4 concurrent appenders, 450,000 measured events in all, with a reactor in each namespace receiving every event.
- **Measures.** Ready is the time from Docker start to the first `200` from `/health/ready`, with a warm page cache. Idle memory is sampled after 30 idle seconds, and peak memory is the highest sample during the workload. Appends is the append rate. Observer catch-up is the time from the first measured append until the reactor in every namespace has received every event, so it includes the appends. Each run uses a new kernel and MongoDB.

The workload is synthetic, so use these figures to compare settings with each other, and your own peak to size the container. CPU-bound figures depend on the host's processors.

### Measure a profile yourself

The Chronicle repository has a [container profile harness](https://github.com/Cratis/Chronicle/blob/main/Benchmarks/Chronicle.Benchmarks/ContainerProfile/README.md) that runs the kernel image under Docker CPU and memory limits with each GC variant, and records time to `/health/ready`, resident and cgroup memory when idle and under an append workload, append throughput, observer catch-up time and out-of-memory outcomes. Run it from a clone of the repository on a host with no other heavy work, and keep the host from sleeping for the whole run, because a busy or sleeping host skews every figure. The harness waits for the host load to drop and skips runs it cannot take on a quiet host. These commands reproduce the table above:

```bash
dotnet build Benchmarks/Chronicle.Benchmarks/Chronicle.Benchmarks.csproj -c Release -p:DisableDockerBuild=true
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile build-images --variants r2r
dotnet run --no-build -c Release --project Benchmarks/Chronicle.Benchmarks -- --container-profile run --matrix core \
  --cpus 1,2,4 --memory 1g,2g,4g \
  --cells 1cpu-1g-server-r2r,1cpu-1g-workstation-r2r,1cpu-1g-server-datas-off-r2r,2cpu-2g-server-r2r,2cpu-2g-workstation-r2r,2cpu-2g-server-datas-off-r2r,4cpu-4g-server-r2r,4cpu-4g-workstation-r2r,4cpu-4g-server-datas-off-r2r
```

Each cell runs three times by default. Compare medians across repetitions and treat a wide spread as a sign of a noisy host, not of the setting.

## Size from your own peak

Memory use depends on your workload. It grows with the number of event stores, namespaces and observers, because observer grains stay resident once they are activated. `cratis.chronicle.observer.resident` reports how many observers each process holds, per event store, namespace and observer type, so you can relate memory to that count (see [Observer state, jobs and append latency](configuration/open-telemetry.md#observer-state-jobs-and-append-latency)).

1. Run your real workload without a hard heap limit.
2. Observe peak resident memory. Use the .NET runtime metrics described in [OpenTelemetry](configuration/open-telemetry.md), or `docker stats`.
3. Set the container memory limit above the observed peak.
4. Set a hard heap limit only when you have a reason to, and keep it above the peak heap size you observed.

A subscribed observer, reactor or reducer keeps roughly 40 KB of heap for as long as it is subscribed. One without a subscription, for example because its client has gone, is released once it has been idle for a while; see [Releasing idle observers](configuration/observers.md#releasing-idle-observers).

A limit below what the kernel needs is not safe. Docker warns that a process can be killed when a container runs out of memory; see [Resource constraints](https://docs.docker.com/engine/containers/resource_constraints/).

## Measured example

These figures come from the Cratis production cluster running Chronicle 19.135.0 over a 7-day window. The version is stated because memory per activation and startup behavior change between kernel versions; measure again after upgrading. Use them to sanity-check your own sizing, not as a replacement for measuring your workload.

| Measure | Value |
| --- | --- |
| Silos | 2 |
| Namespaces | 30 |
| Grain activations | About 42,000 across both silos |
| Event rate | 0.26 events/s on average |
| Working set per silo | Average 2.9 to 3.4 GiB, p95 about 4.2 GiB, maximum 5.2 GiB |
| Live GC heap | About 0.2 GiB baseline plus about 50 KB per activation |
| CPU | Average 1.3 to 1.5 cores, p95 2.6 cores, maximum 3.6 cores during cold start |
| Container memory limit | 6 GiB |

Traffic did not drive memory here: at a quarter of an event per second, the heap was dominated by resident grain activations.

### How the numbers were taken

The kernel exports the .NET runtime meter and the `Microsoft.Orleans` meter through OpenTelemetry; see [OpenTelemetry](configuration/open-telemetry.md). With the Prometheus names the OpenTelemetry Collector's Prometheus exporter or Prometheus's OTLP receiver produce, these queries reproduce the table:

| Metric | Source instrument | What it shows |
| --- | --- | --- |
| `dotnet_process_memory_working_set_bytes` | `dotnet.process.memory.working_set` | Resident memory of the process, which the container limit is measured against |
| `dotnet_gc_last_collection_heap_size_bytes` | `dotnet.gc.last_collection.heap.size` | Live heap after the last collection, one series per GC generation |
| `orleans_catalog_activations` | `orleans-catalog-activations` | Grain activations on each silo |
| `chronicle_event_sequences_appended_events_total` | `chronicle-event-sequences-appended-events` | Events appended, by namespace and event type |
| `dotnet_process_cpu_time_seconds_total` | `dotnet.process.cpu.time` | CPU time the process used, by `cpu_mode` |
| `dotnet_thread_pool_queue_length` | `dotnet.thread_pool.queue.length` | Work items waiting in the .NET thread pool queue |

The appended-events name is the default legacy name. With `Telemetry:MetricNames=Convention` it is `cratis_chronicle_event_sequence_appended_events_total`; see [telemetry names](configuration/telemetry-names.md).

```promql
# Working set per silo: average, p95 and maximum over the window
avg_over_time(dotnet_process_memory_working_set_bytes[7d])
quantile_over_time(0.95, dotnet_process_memory_working_set_bytes[7d])
max_over_time(dotnet_process_memory_working_set_bytes[7d])

# Live heap per silo, summed over the GC generations
sum by (instance) (dotnet_gc_last_collection_heap_size_bytes)

# Activations per silo
sum by (instance) (orleans_catalog_activations)

# Silos: the number of silos reporting
count(count by (instance) (dotnet_process_memory_working_set_bytes))

# Event rate across the cluster, averaged over the window, in events per second
sum(increase(chronicle_event_sequences_appended_events_total[7d])) / (7 * 24 * 3600)

# CPU per silo in cores: average, p95 and maximum over the window
avg_over_time((sum by (instance) (rate(dotnet_process_cpu_time_seconds_total[5m])))[7d:5m])
quantile_over_time(0.95, (sum by (instance) (rate(dotnet_process_cpu_time_seconds_total[5m])))[7d:5m])
max_over_time((sum by (instance) (rate(dotnet_process_cpu_time_seconds_total[5m])))[7d:5m])

# Peak thread pool queue length per silo
max_over_time(dotnet_thread_pool_queue_length[7d])
```

Two rows do not come from a metric. The namespace count is the number of namespaces listed for the event stores in the Workbench. The container memory limit is the value set in the deployment.

Adjust the `instance` label to the one your scrape configuration uses for a silo. To find your own baseline, read the live heap right after a silo starts, before observers activate, while the activation count is still close to zero. Divide the live heap, less that baseline, by the activation count to get the per-activation figure for your own workload.

### Rule of thumb

Estimate the live heap of one silo as about 0.2 GiB plus about 50 KB for each grain activation it holds. For the example above, about 21,000 activations per silo, if spread evenly, come to about 1 GiB on top of the baseline. The working set runs well above the live heap, so set the container limit from the observed working-set peak and leave headroom for cold start.

Activations grow with the number of event stores, namespaces and observers, not with event throughput in this workload. Adding namespaces adds activations even when they receive few events.

### CPU is a cold-start concern

Steady-state CPU says little about what the kernel needs. On the cluster above, a limit of 2 CPUs starved the .NET thread pool during startup and made startup fail, even though average use stayed between 1.3 and 1.5 cores. The thread pool queue reached 64,000 work items within the 7-day window. Size the CPU request and limit from the cold-start peak, 3.6 cores here, rather than from the average. [Rolling deploy](rolling-deploy.md) covers how restarts and rollouts interact with startup.

### The memory budget does not cap resident state

The [memory budget](configuration/memory.md) makes heavy, short-lived work such as reducing large read-model states wait for room. By default it covers 40 % of the memory available to the process, which is the GC heap hard limit or the container limit when there is one (`memory.budgetPercentOfAvailableMemory`). It does not bound the memory held by resident grain activations, so it is not a substitute for sizing the container from the measured working set.

## Choosing a tenancy topology

You can serve several tenants from one kernel, with a [namespace per tenant](../tenancy/index.md), or run a kernel for each tenant. The choice changes isolation, memory, upgrades and storage.

| Concern | Shared kernel, namespace per tenant | Kernel per tenant |
| --- | --- | --- |
| Isolation | Namespaces separate events, observer state and read models, by default in separate databases. Tenants share the process, its CPU, memory and thread pool, so one tenant's backlog or cold start slows the others. Your application authorizes each tenant | Each tenant has its own process and resources. One tenant's load or failure stays with that kernel |
| Memory | One baseline for all tenants. Activations still grow with each namespace | The baseline, about 0.2 GiB in the example above, is paid once per kernel: baseline × kernel count. Each kernel also needs its own cold-start headroom, so the limits add up to more than one shared limit |
| Upgrade blast radius | One rollout moves every tenant to the new version at once, and a rollback does the same | Tenants upgrade one kernel at a time, at the cost of running a rollout per kernel |
| Storage | One storage backend holds every tenant: by default two databases per namespace, one for events and observer state and one for read models. Size it, back it up and connect to it for all tenants together. Use [storage connection resolution](configuration/storage-connection-resolution.md) when tenants need their own databases, servers or identities | Each kernel has its own storage configuration, System event store and users. Give each kernel storage that no other kernel writes to |

A shared kernel is cheaper when tenants are many and small. A kernel per tenant fits when a tenant needs process-level isolation, its own upgrade schedule or its own resource limits.

## Shutdown

Set the termination grace period to at least the 10 second host timeout. Observer progress that is not saved before the process stops is delivered again after the restart. See [Rolling deploy](rolling-deploy.md) and [Graceful shutdown](configuration/root-properties.md#graceful-shutdown).

## Storage on container platforms

Do not use SQLite on platforms where the container file system is lost on restart. See [Container Apps, App Service and Cloud Run](azure-container-hosting.md).
