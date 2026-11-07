---
title: Choose a Chronicle observer recovery
description: Chronicle retry preserves partition state; replay rebuilds from history. Clear quarantine after fixing the cause and check for repeated side effects.
---

Cratis Chronicle recovers a failed observer partition by retrying from the failure or replaying history to rebuild state. Retry keeps the state already accumulated; replay processes earlier events again. Choose between them based on whether that state is still correct, and check which handlers and external effects will run again.

## Failed and quarantined partitions

A failed partition holds back processing for that partition after an event could not be handled. Other partitions can continue. Chronicle retries with exponential backoff, increasing the delay between attempts up to the configured maximum. An observer subscription, such as one following a redeploy, also requests recovery of eligible failed partitions.

Once the retry budget is exhausted, the partition is quarantined and automatic retries stop. `MaxRetryAttempts` defaults to 10; 0 means unlimited retries. Configure the budget and delays in [observer configuration](/chronicle/hosting/configuration/observers/).

The observer itself can also be quarantined when configured failure thresholds are reached. That stops recovery across the observer, not just one partition. Check both levels before assuming that releasing a single partition will get it moving.

## Choose an action

Resolve the cause before restarting processing. A transient dependency failure may need no code change; a handler bug needs a fix and redeployment. If earlier processing wrote incorrect state, retry alone leaves that state in place.

| Situation | Action | Consequence |
|---|---|---|
| The cause is resolved and the existing state is correct | [Retry partition](/cli/chronicle/observers/#retry-partition) | Resumes at the failed event without resetting accumulated state. The handler can repeat an external effect that completed before the failure. |
| One partition's state needs rebuilding | [Replay partition](/cli/chronicle/observers/#replay-partition) | Processes that partition's history from its first event. Other partitions are not rebuilt. |
| A logic change requires rebuilding all partitions | [Full observer replay](/cli/chronicle/observers/#replay) | Processes history across the observer. The wider scope can repeat work that was already correct. |
| A partition has exhausted its retry budget | [Clear partition quarantine](/cli/chronicle/observers/#clear-partition-quarantine) | Resets its retry budget, keeps attempt history and, by default, starts a retry. It does not repair state. |
| The observer itself is quarantined | [Clear observer quarantine](/cli/chronicle/observers/#clear-quarantine) | Lets the observer resume, but leaves individually quarantined partitions quarantined. |

For several failed partitions with the same resolved cause, [`retry-failed-partitions`](/cli/chronicle/observers/#retry-failed-partitions) requests recovery together. It requires Chronicle 19.80.0 or later and returns before recovery finishes. Inspect the remaining failures rather than treating the command result as completion.

Clearing partition quarantine requires Chronicle 19.29.0 or later. Use `--no-retry` if you want to clear it without requesting an immediate retry. Clearing either level of quarantine does not fix the cause. If both levels are quarantined, release the observer first, then address its partitions.

### Replay and side effects

Both retry and replay can deliver an event that a handler has seen before. Make external effects idempotent: processing the same delivery again must not create an unintended duplicate, such as a second payment or notification. A handler can finish an external call and then fail before Chronicle records its progress. [Delivery identity](/chronicle/reactors/delivery-identity/) explains how to recognize that repeated delivery.

Replay has an additional boundary: only replayable observers can replay, and reactor replay rules determine which handler runs. In the .NET client, `[OnceOnly]` on a reactor class makes the whole reactor non-replayable; on a method it excludes that handler during replay. A `[Replay]` handler can provide different behavior for replayed events. Neither attribute prevents duplicate work during an ordinary failed-partition retry. See [Once only](/chronicle/reactors/once-only/) and [Replay](/chronicle/reactors/replay/).

For a projection, a full replay rebuilds the read model. Projections with `Global` or `Both` scope reject namespace-local replay because their read-model storage is shared across namespaces and needs a coordinated rebuild. For supported replays, follow [Rebuild a read model](/chronicle/scenarios/rebuild-a-read-model/), then check the job result and the resulting data. Request acceptance is not evidence that the rebuild has completed.

## From application code

A client can also retry a failed reactor partition or clear its quarantine programmatically. See [Retry one reactor partition from a client](/chronicle/troubleshooting/#retry-one-reactor-partition-from-a-client).

## Next

If you have not identified the failed event yet, start with [Diagnostics](/chronicle/operations/diagnostics/). Once the cause is understood, follow [Recover a failed partition](/chronicle/operations/recover-a-failed-partition/) to apply the recovery and verify its output.
