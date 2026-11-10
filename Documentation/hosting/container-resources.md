---
title: Size the kernel for a container
description: Memory, CPU and garbage collection settings for the Chronicle kernel image, how to size a container from your own peak with a measured example, and how to choose between a shared kernel and a kernel per tenant.
---

This page describes how the Chronicle kernel image runs today, which .NET garbage collection (GC) settings you can change, and how to size a container limit from your own workload. For the general production checklist, see [Production deployment](production.md). For single-instance platforms, see [Container Apps, App Service and Cloud Run](azure-container-hosting.md).

## What the image runs with

The production image is built with these settings:

- **Server GC.** The kernel project sets `ServerGarbageCollection` to `true`.
- **Conserve memory.** The runtime configuration sets `System.GC.ConserveMemory` to `5`. A higher value makes the GC trade more frequent collections, and possibly longer pauses, for a smaller heap.
- **Self-contained ReadyToRun publish.** The release workflow publishes `linux-x64` and `linux-arm64` with `PublishReadyToRun=true` and `--self-contained`.
- **Base image.** `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`. The image runs `./Cratis.Chronicle.Server` directly as the entry point.
- **Host shutdown timeout.** The host waits 10 seconds for a graceful stop.

The kernel does not set any other GC option, such as `GCHeapHardLimit`, `GCDynamicAdaptationMode` or `GCHeapCount`.

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
| `DOTNET_GCDynamicAdaptationMode` | `System.GC.DynamicAdaptationMode` | `1` enabled, `0` disabled | Turns dynamic adaptation to application sizes (DATAS) on or off. DATAS aims to keep the heap roughly proportional to the long-lived data size. |

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

## Size from your own peak

Memory use depends on your workload. It grows with the number of event stores, namespaces and observers, because observer grains stay resident once they are activated.

1. Run your real workload without a hard heap limit.
2. Observe peak resident memory. Use the .NET runtime metrics described in [OpenTelemetry](configuration/open-telemetry.md), or `docker stats`.
3. Set the container memory limit above the observed peak.
4. Set a hard heap limit only when you have a reason to, and keep it above the peak heap size you observed.

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
