---
title: Recovering from failures
description: How Chronicle retries a failed partition, when it quarantines it, and which recovery action fits: retry, partition replay, full observer replay or clearing quarantine.
---

An observer handles events one event source partition at a time. When a handler throws, only that partition pauses, so Chronicle never silently skips an event. This page explains what Chronicle does on its own and which action you pick when it stops.

## Failed and quarantined partitions

A **failed** partition is paused at the event that threw. Chronicle retries it automatically with an exponential backoff, and tries again when the observer subscribes, for example after a redeploy.

After the retry budget is used up, the partition is **quarantined**. The default budget is 10 attempts, and 0 means unlimited. A quarantined partition is not retried automatically. The limits are set in [observer configuration](/chronicle/hosting/configuration/observers/).

## Choose an action

| Situation | Action | What it does |
|---|---|---|
| A transient error, or you fixed the bug and redeployed | **Retry partition** | Resumes at the failed event and keeps the accumulated state. See [`retry-partition`](/cli/chronicle/observers/#retry-partition) and [`retry-failed-partitions`](/cli/chronicle/observers/#retry-failed-partitions). |
| That partition's state is wrong | **Replay partition** | Rebuilds that one partition from its first event. See [`replay-partition`](/cli/chronicle/observers/#replay-partition). |
| You changed a projection or handler logic | **Full observer replay** | Rebuilds every partition. See [`replay`](/cli/chronicle/observers/#replay). |
| The partition or observer is quarantined | **Clear quarantine** | Lets it run again. See [`clear-partition-quarantine`](/cli/chronicle/observers/#clear-partition-quarantine) and [`clear-quarantine`](/cli/chronicle/observers/#clear-quarantine). |

Clear quarantine covers both a quarantined partition and a quarantined observer. Clearing it does not fix the cause. If the handler still throws, the partition fails again.

### Replay and side effects

A replay runs handlers again. A reactor with side effects must be idempotent, and replaying a partition is no escape for a handler guarded by `[OnceOnly]`. See [Once only](/chronicle/reactors/once-only/) and [Replay](/chronicle/reactors/replay/).

For a projection, a full replay rebuilds the read model. See [Rebuild a read model](/chronicle/scenarios/rebuild-a-read-model/).

## From application code

A client can also retry a failed reactor partition or clear its quarantine programmatically. See [Retry one reactor partition from a client](/chronicle/troubleshooting/#retry-one-reactor-partition-from-a-client).

## Next

- [Diagnostics](/chronicle/operations/diagnostics/) to find which observer and partition failed.
- [Recover a failed partition](/chronicle/operations/recover-a-failed-partition/) for the decision path from failure to done.
