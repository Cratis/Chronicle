---
title: Recover a failed partition
description: Decide between retry, partition replay, full observer replay and clearing quarantine for an observer that stopped on a failing partition, and know when you are done.
---

An observer stopped on one partition and the other partitions carry on. This page tells you which recovery to pick. The commands live in the Cratis CLI runbook [Fix a stuck observer](/cli/scenarios/fix-a-stuck-observer/), and this page does not repeat them.

## Before you start

Find the observer and the partition, and read the error. See [Diagnostics](/chronicle/operations/diagnostics/). Fix the cause in your code first. A recovery without a fix fails again.

## Pick the recovery

1. **Is the partition quarantined?** Chronicle stops retrying after the retry budget. [Clear the quarantine](/cli/chronicle/observers/#clear-partition-quarantine) first, otherwise nothing you do next runs.
2. **Was the error transient, or did you fix and redeploy the handler?** [Retry the partition](/cli/chronicle/observers/#retry-partition). It resumes at the failed event and keeps the state it had.
3. **Is the state of that partition wrong?** [Replay the partition](/cli/chronicle/observers/#replay-partition). It rebuilds that partition from its first event.
4. **Did you change the logic of a projection or handler for every partition?** [Replay the observer](/cli/chronicle/observers/#replay). See [Rebuild a read model](/chronicle/scenarios/rebuild-a-read-model/).

For why each action behaves as it does, see [Recovering from failures](/chronicle/operations/recovering-from-failures/).

## Done when

- The partition no longer appears in [`failed-partitions list`](/cli/chronicle/failed-partitions/#list).
- The observer is running and its position moves forward.
- For a replay, [the job](/cli/chronicle/jobs/) has finished and the read model matches the expected state.
- Any side effects that ran twice were safe, because the reactor is idempotent. See [Delivery identity](/chronicle/reactors/delivery-identity/).
