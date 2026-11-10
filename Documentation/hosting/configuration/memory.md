---
title: Configure memory management in Cratis Chronicle Server
description: "Configure the Chronicle memory budget so heavy work, such as reducing large read models, waits for room, and learn which memory metrics the server reports."
---
The `memory` section controls the memory budget. Work that needs a lot of memory for a short time, such as reducing a read model whose state is tens of megabytes, reserves memory from the budget before it runs. When the budget is full the work waits its turn, so many large reductions no longer run at once and exhaust the heap.

## Example configuration

Put these settings at the root of `chronicle.json`, not inside a `Cratis` or `Chronicle` wrapper. The following example shows the defaults:

```json
{
    "memory": {
        "enabled": true,
        "budgetPercentOfAvailableMemory": 40,
        "budgetPercentUnderPressure": 50,
        "maximumWaitSeconds": 120,
        "minimumReservationBytes": 1048576,
        "reducerCostMultiplier": 4
    }
}
```

| Setting | Default | Meaning |
| --- | --- | --- |
| `enabled` | `true` | Whether work waits for room in the budget. Set to `false` to turn the gate off. The memory metrics are still reported. |
| `budgetPercentOfAvailableMemory` | `40` | Share, from 1 to 100, of the memory available to the process that the budget covers. Available memory is the GC heap hard limit or the container limit when there is one, otherwise the memory of the machine. The rest is left for the runtime, caches, connections and work too small to wait. |
| `budgetPercentUnderPressure` | `50` | Share, from 1 to 100, of the budget that remains while the garbage collector reports a high memory load. The budget shrinks and grows with the memory pressure of the process. |
| `maximumWaitSeconds` | `120` | Seconds a request waits for room before it is let through regardless. `0` or less waits indefinitely. This keeps one stuck holder, for example a client that does not answer, from stopping all other work. |
| `minimumReservationBytes` | `1048576` (1 MiB) | Estimated size below which work does not reserve memory at all. |
| `reducerCostMultiplier` | `4` | How many times the size of a reducer's state a reduce is expected to hold at its peak. A reduce holds the stored state, the state sent to the client, the state it returns and the changes written, in several representations at once. |

Environment variables use the `Cratis__Chronicle__Memory__` prefix, for example `Cratis__Chronicle__Memory__Enabled` and `Cratis__Chronicle__Memory__BudgetPercentOfAvailableMemory`.

## How the budget works

- Requests are served strictly in order. A small request does not overtake a large one that is waiting.
- A request larger than the whole budget runs alone, once nothing else holds memory.
- A reduce reserves for the state size its partition had the last time it was reduced, times `reducerCostMultiplier`. The first reduce of a partition after a restart has no size to go by, so it accounts for its state once it has been read, and waits then if there is no room.
- The reservation is released when the reduce is done, whether it succeeded or failed.

## Sizing the container

The budget is a share of the memory Chronicle can use, so give Chronicle a memory limit that fits the largest read model states you reduce. A reduce of a state of *S* bytes can need roughly 8 to 10 times *S* at its peak. A container limit of 1 GiB is too small for states around 50 MB. Start at 2 GiB, or set `DOTNET_GCHeapHardLimitPercent` (for example `55`) so that the heap leaves room for memory the garbage collector does not manage.

## Metrics

Chronicle reports these through [OpenTelemetry](open-telemetry.md) on the `Cratis.Chronicle` meter.

| Metric | Meaning |
| --- | --- |
| `cratis.chronicle.memory.heap_size` | Size of the managed heap after the last collection. |
| `cratis.chronicle.memory.committed` | Memory committed by the garbage collector. |
| `cratis.chronicle.memory.available` | Memory available to the process. |
| `cratis.chronicle.memory.load` | Memory in use as seen by the garbage collector. |
| `cratis.chronicle.memory.fragmentation` | Fragmented heap memory. |
| `cratis.chronicle.memory.pause_time_ratio` | Percentage of time spent paused in garbage collection. |
| `cratis.chronicle.memory.working_set` | Working set of the process. |
| `cratis.chronicle.memory.allocated` | Bytes allocated. |
| `cratis.chronicle.memory.gen2_collections` | Full collections. |
| `cratis.chronicle.memory.budget.capacity` | Current capacity of the budget. |
| `cratis.chronicle.memory.budget.reserved` | Memory currently reserved. |
| `cratis.chronicle.memory.budget.waiting` | Reservations waiting for room. |
| `cratis.chronicle.reducer.state_size` | Estimated size of reducer states, as a histogram. |

A budget with `waiting` that stays above zero, or a growing wait time, means reductions are queuing for memory: raise the container limit, or lower `reducerCostMultiplier` if your states are known to reduce cheaply.
