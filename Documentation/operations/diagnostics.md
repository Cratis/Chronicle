---
title: Diagnostics
description: What to look at, in order, when an observer stops: the CLI diagnosis, the observer list, the failed partition and the background jobs.
---

Look at these in order. Each step narrows the question from "something is wrong" to "this partition failed at this event".

1. **Run the diagnosis.** [`cratis chronicle diagnose`](/cli/chronicle/diagnose/) is the diagnostic entry point.
2. **List the observers.** [`observers list`](/cli/chronicle/observers/#list) shows every registered observer.
3. **Show the failed partition.** [`failed-partitions show`](/cli/chronicle/failed-partitions/#show) gives the error message and the sequence number the partition stopped at. [`failed-partitions list`](/cli/chronicle/failed-partitions/#list) shows every failed partition.
4. **Check the jobs.** [`jobs`](/cli/chronicle/jobs/) shows replays and other background work that may still be running. Wait for a running replay before you start another.

Traces and alerts give the same signal earlier. See [Alerting on observer failures](/chronicle/hosting/alerting-on-observer-failures/) and [Troubleshooting](/chronicle/troubleshooting/) for missing spans.

## Using the Workbench

The [Workbench](/chronicle/workbench/) shows observer and failed-partition state in a local browser. It also offers some recovery actions: retry on a failed partition, and replay, fast-forward, clear quarantine and remove on an observer. Before you use any of them, decide which recovery fits from [Recovering from failures](/chronicle/operations/recovering-from-failures/). That page explains when each action is appropriate and links it to its CLI page, which is the scriptable path.

## Next

- [Recovering from failures](/chronicle/operations/recovering-from-failures/)
- [Recover a failed partition](/chronicle/operations/recover-a-failed-partition/)
