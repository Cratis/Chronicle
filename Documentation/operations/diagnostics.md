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

## The Workbench is for inspection

The [Workbench](/chronicle/workbench/) shows runtime state in a local browser. Use it to look. Choose and run a recovery from the path in [Recovering from failures](/chronicle/operations/recovering-from-failures/), which links every action to its CLI page.

## Next

- [Recovering from failures](/chronicle/operations/recovering-from-failures/)
- [Recover a failed partition](/chronicle/operations/recover-a-failed-partition/)
