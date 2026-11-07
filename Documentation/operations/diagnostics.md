---
title: Diagnose a stopped Chronicle observer
description: Find why a Chronicle observer stopped by checking its scope, failed partition, error history and jobs before choosing a recovery action.
---

To diagnose a stopped observer in Cratis Chronicle, identify its event store and namespace, then inspect the failed partition and any recovery jobs. An observer processes events to build a read model or run application behavior; a failure can leave that output stale even while other partitions continue.

Use the [Cratis CLI](/cli/) for the checks below. Keep the event store and namespace consistent across commands so you inspect the same deployment data throughout.

1. Run [`cratis chronicle diagnose`](/cli/chronicle/diagnose/) for a health report. By default it checks the selected event store and namespace, not every scope. Read any **Could not check** results: a query that failed is not evidence that there are no failures.
2. Use [`observers list`](/cli/chronicle/observers/#list) to find the observer in that scope and inspect its state and sequence position. An active observer can still have a failed partition, so continue to the partition checks.
3. Use [`failed-partitions list`](/cli/chronicle/failed-partitions/#list) to find the partition, then [`failed-partitions show`](/cli/chronicle/failed-partitions/#show) to read its failing sequence number, exception and attempt history. Record the observer ID and partition ID for recovery.
4. Check [`jobs`](/cli/chronicle/jobs/) for replays and other background work. Wait for an existing replay to finish and inspect its result before requesting another; a running job can explain why the output has not caught up yet.

For earlier notification, configure [alerts on observer failures](/chronicle/hosting/alerting-on-observer-failures/). If the symptom is missing trace spans rather than a recorded partition failure, continue with [Troubleshooting](/chronicle/troubleshooting/).

## Using the Workbench

The [Workbench](/chronicle/workbench/) provides browser views of observers and failed partitions. Inspect the same event store, namespace, observer and partition there if you prefer a visual route.

Recovery actions change processing state. Before using retry, replay or clear quarantine, read [Recovering from failures](/chronicle/operations/recovering-from-failures/) to choose the scope and understand what runs again. The Workbench also offers fast-forward and remove; these are not substitutes for repairing a failed handler.

## Next

Once you have the error and partition ID, follow [Recover a failed partition](/chronicle/operations/recover-a-failed-partition/) to apply a fix and confirm recovery.
