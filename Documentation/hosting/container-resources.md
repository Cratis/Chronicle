---
title: Size the kernel for a container
description: Memory, CPU and garbage collection settings for the Chronicle kernel image, which settings you can change, and how to size a container from your own peak.
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

## Shutdown

Set the termination grace period to at least the 10 second host timeout. Observer progress that is not saved before the process stops is delivered again after the restart. See [Rolling deploy](rolling-deploy.md) and [Graceful shutdown](configuration/root-properties.md#graceful-shutdown).

## Storage on container platforms

Do not use SQLite on platforms where the container file system is lost on restart. See [Container Apps, App Service and Cloud Run](azure-container-hosting.md).
