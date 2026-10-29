---
description: Configure Chronicle observer retry behavior, timeouts, watchdog monitoring and fan-out, and how definition changes are evolved.
---
# Observers

Observer configuration controls retry behavior, timeouts, watchdog monitoring, and how events fan out to scaled-out client instances.

## Example configuration

```json
{
  "observers": {
    "subscriberTimeout": 30,
    "abandonedDeliveryHold": 300,
    "maxRetryAttempts": 10,
    "backoffDelay": 1,
    "exponentialBackoffDelayFactor": 2,
    "maximumBackoffDelay": 600,
    "quarantineOnFailedPartitionCount": 0,
    "quarantineOnFailedPartitionPercentage": 0.0,
    "definitionEvolution": "Automatic",
    "watchdogInterval": 60,
    "maxConcurrentPartitions": 32,
    "fanOutStrategy": "round-robin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| subscriberTimeout | number | 30 | How many seconds an observer waits for its subscriber to answer a batch before giving up on it. Not capped by `clustering.responseTimeout`; bounded at 1 hour. `0` waits indefinitely (up to that bound). See [Subscriber timeout](#subscriber-timeout) |
| abandonedDeliveryHold | number | 300 | Maximum additional seconds to hold an abandoned gRPC client delivery. Positive values are at least `subscriberTimeout`. `0` or less disables fencing; a disabled subscriber timeout also disables it. See [Abandoned client deliveries](#abandoned-client-deliveries) |
| maxRetryAttempts | number | 10 | Maximum retry attempts for failed partitions (0 = infinite). The limit applies per retry budget: clearing the quarantine of a partition starts a new budget, so it gets this many attempts again |
| quarantineOnFailedPartitionCount | number | 0 | Quarantine the observer once this many of its partitions have failed (0 = never) |
| quarantineOnFailedPartitionPercentage | number | 0.0 | Quarantine the observer once this share of its observed partitions have failed (0.0 = never) |
| backoffDelay | number | 1 | Initial backoff delay in seconds |
| exponentialBackoffDelayFactor | number | 2 | Exponential backoff multiplier |
| maximumBackoffDelay | number | 600 | Maximum backoff delay in seconds |
| definitionEvolution | string | Automatic | Controls whether projection and reducer definition changes apply `Automatic`, `PartialOnly`, or `Manual` evolution. See [Definition evolution](#definition-evolution) |
| replayOnDefinitionChange | boolean | false | Controls automatic replay for reactors and webhooks. Projection and reducer changes use `definitionEvolution` |
| watchdogInterval | number | 60 | Interval in seconds between watchdog checks; the watchdog verifies connected clients are still active, running jobs (replay and catch-up) are still progressing, and `NextEventSequenceNumber` is up-to-date |
| maxCatchupRecoveryAttempts | number | 5 | How many watchdog checks in a row may retry a stuck catch-up preparation, or a recovery that failed after quarantine was cleared, before the watchdog quarantines the observer. `0` or less quarantines on the first retry. See [Ending quarantine](#ending-quarantine) |
| maxConcurrentPartitions | number | 32 | Upper bound on how many job steps (replay and catch-up work) run in parallel. The effective limit is the smaller of this value and `jobs.maxParallelSteps`. Despite the name it does not limit live event delivery. See [Job throttling](job-throttling.md) |
| fanOutStrategy | string | round-robin | Strategy for distributing events across multiple connected instances of the same client. `round-robin` distributes deterministically by partition key, keeping every partition sticky to one instance and preserving per-partition ordering. `random` picks a random instance per delivery |

## Definition evolution

Chronicle compares every newly registered projection and reducer definition with its stored definition. It then chooses the minimum operation it can prove correct:

| Classification | Behavior |
| --- | --- |
| No action | A newly consumed event type has no historical events, so existing read models cannot change |
| Partial replay | A reducer adds event types that occur only after its previously consumed events for each affected event source. Chronicle applies only those new event types to the affected event sources |
| Full replay | A projection adds event types with historical events, or the mapping, reducer implementation fingerprint, filter, key, join, child structure, or another order-dependent part changed. Chronicle rebuilds the complete observer |

`Automatic` applies every classification. `PartialOnly` applies no-action and partial plans, but creates a replay recommendation when a full replay is required. `Manual` creates a recommendation for both partial and full plans. No-action plans never create recommendations because history cannot be affected.

Projection definition changes requiring historical backfill always use full replay, even for independent property mappings. Replaying selected historical events into the live container can reject writes behind its watermark or apply contributions twice. With `PartialOnly` or `Manual`, these changes therefore create a full-replay recommendation. Explicit partition replay is unchanged.

Reducer registrations include a fingerprint of their reducer methods. Changing reducer code therefore triggers classification even when its event-type subscription is unchanged. Reducer methods are imperative, so Chronicle only chooses a partial reducer replay when history proves every newly consumed event follows all previously consumed events within its event source. Interleaved history falls back to full replay.

## Subscriber timeout

Every path that hands events to an observer's subscriber — live delivery, catch-up and replay — waits at most
`subscriberTimeout` seconds for an answer. When it elapses, the batch is recorded as a failed partition of kind
`Timeout` and the partition retries with the usual backoff.

Giving up abandons the wait, not the work. For gRPC reactors and reducers, Chronicle holds the subscriber turn
open after the timeout and fences that partition before recovery can send another batch.

The setting is independent of `clustering.responseTimeout`. Subscriber calls carry their own Orleans response
timeout of **1 hour**, so a `subscriberTimeout` of, for example, `600` waits the full 10 minutes without raising
the response timeout for every other call in the cluster. When a subscriber call does hit that 1-hour ceiling, the
failure says so (`The Orleans response timeout fired after ...`) instead of naming the configured subscriber
timeout.

Set it to `0` to wait indefinitely. This also disables abandoned-delivery fencing. Both `0` and values above one
hour are bounded by the 1-hour ceiling, and Chronicle logs a warning at startup when that applies.

## Abandoned client deliveries

`abandonedDeliveryHold` defaults to **300 seconds after the delivery deadline**. A positive value smaller than
`subscriberTimeout` uses the subscriber timeout instead. Set it to `0` or less to restore immediate, unfenced
recovery. Kernel-owned subscribers and webhooks keep their existing behavior.

Live delivery records the timeout without waiting inline. Retry, catch-up and replay wait for a barrier on the
**original subscriber activation**, not a replacement client. Superseding a connected client or removing its target
from the subscription does not prove its work ended. Unaffected partitions can continue. A failed or timed-out
barrier call — including an unavailable original silo — keeps the fence until the maximum hold unless a later
barrier succeeds. Chronicle never treats a placement exception as proof that the old activation drained.

Each dispatch carries optional deadlines in its internal subscriber context; no gRPC contract or client upgrade
is required. A queued delivery that arrives after its deadline is rejected before reaching the client mediator.
Results received after the deadline are discarded, and a late reducer result cannot write to the sink. Pending
turns end when the client answers, disconnects, or the hold expires. Timeout failure attempts persist the original
target identity and release deadline. After reactivation, an old or unparseable identity is held conservatively for
`abandonedDeliveryHold` from the failure time; recovery never substitutes the current subscription target.

:::caution[A maximum hold is not cancellation]
After the maximum hold, a disconnected or wedged client might still be running user code. Fencing prevents overlap
within this bounded window; it does not provide exactly-once side effects. Keep external side effects idempotent.
Live fences survive an ordinary failure clearance in memory, but clearing their persisted failure history followed
by a kernel restart loses that history. Rolling back to an older kernel also loses the fence behavior.
:::

Use the [abandoned-delivery logs and metrics](../alerting-on-observer-failures.md#abandoned-client-deliveries) to tell
a slow client from a failed one before reducing the hold.

## What a failed partition says went wrong

Every attempt recorded against a failed partition carries a **kind**, so an observer that is wrong can
be told apart from one that was only waiting on a busy kernel:

| Kind | Meaning |
| --- | --- |
| `Handling` | The subscriber failed while handling the events. This is the failure that means something is wrong |
| `Timeout` | The call to the subscriber did not come back in time. The events were never rejected — the kernel ran out of patience waiting, which says the system was congested |
| `Disconnected` | The subscriber was gone by the time the events reached it |
| `Unknown` | Nothing classified the failure. Every attempt recorded before failures carried a kind reads back as this |

A partition whose last attempt is a `Timeout` does not count toward the quarantine thresholds above.
Quarantining stops retries and needs an operator to undo, which is the right answer for an observer
that is wrong and the wrong answer for one waiting on congestion that will clear on its own.

Webhook retries use these same budget and delay settings, with bounded ±20% jitter and a persisted
`RetryBackoff` deadline. `maxRetryAttempts: 0` still means unlimited retries. For webhooks, the
observer-quarantine thresholds count parked non-timeout partitions, not transient partitions still
waiting for retry. Receiver `Retry-After` and open-circuit deferrals consume no retry attempts.
See [Webhook delivery policy](../../webhooks/index.mdx#receiver-back-pressure-and-permanent-failures).

## Ending quarantine

An observer's quarantine ends only through one of these actions:

1. An operator clears it with `cratis chronicle observers clear-quarantine`, from the Workbench, or through
   the corresponding clear-quarantine API.
2. The observer receives a fresh subscription from its client or from the Kernel, as listed in the table below.
   The Kernel re-establishing an event store subscription or reconciling a webhook at startup does not end quarantine.
3. For an eligible webhook delivery failure, an [automatic recovery probe](../../webhooks/index.mdx#automatic-recovery-from-quarantine) succeeds and durably records progress.

What can establish a fresh subscription depends on the observer:

| Observer | Subscribed again |
| --- | --- |
| Reactors and reducers of an application | When an instance of the application's client connects or reconnects, including after a Kernel restart, and when the client registers a changed definition |
| Projections | When the Kernel starts, when a client registers a changed definition, and when a namespace is added |
| Webhooks | When a client registers a new or changed webhook, when a webhook is added or edited (target URL, headers, authorization or event types), and when a namespace is added |
| Event store subscriptions | When the Kernel starts, when a namespace is added, and by the check the Kernel runs every minute, which subscribes one again if it is no longer subscribed or its event types changed. None of these ends quarantine; clear it with `cratis chronicle observers clear-quarantine` or from the Workbench |
| The Kernel's own reactors and pattern capture | When the Kernel starts, when an event store is added, and when a namespace that already holds events is added. Pattern capture is also subscribed again when a client registers new event types |

These actions do **not** end quarantine:

- Watchdog ticks, including checks for missing jobs and stranded catch-up preparation.
- Catch-up or replay completion, including completion for individual partitions.
- Unsubscription or observer grain deactivation and reactivation.
- Webhook startup reconciliation, which preserves quarantine and the durable recovery probe.
- Automatic event-store-subscription reconciliation, whether triggered by startup, manager reactivation,
  source availability, namespace notifications, definition reconciliation, or the minute check.

A Kernel restart ends quarantine only for observers that receive a fresh subscription as listed above;
webhook startup reconciliation preserves quarantine, and event store subscriptions remain quarantined until an operator clears them.

Completing catch-up or replay still persists progress and clears completed-work markers without resuming the
observer. Clearing quarantine then re-evaluates remaining work from the recorded position. For a subscribed
observer, clearing resumes paused non-replay jobs, including catch-up jobs, and retries failed partitions that
are not individually quarantined, subject to the configured retry limits. Recovery can quarantine the observer
again if the underlying problem remains. If that recovery fails, for example because the job store is briefly
unavailable, the observer is left **Disconnected** rather than **Quarantined**. The watchdog retries the recovery
on each check, and after `maxCatchupRecoveryAttempts` failed retries in a row it quarantines the observer again so
you can see it and clear it once the cause is fixed. An observer without a subscription follows the same routing as
activation, including how pending replay is handled, and cannot deliver events until a subscription is established.
Quarantine does not cancel already-running catch-up or replay jobs, and partition completion can still start
required partition continuation work without ending the observer's quarantine.

Clearing observer quarantine does not clear failed partitions or their separate quarantine status. Failed
partitions stay recorded until they recover or are cleared.

## Scaled-out clients

When multiple instances of the same client application connect, its reactors and reducers all
subscribe to the same observer. Chronicle fans event delivery out across the instances using the
configured `fanOutStrategy`. If an instance disconnects, it is removed immediately and its
partitions are redistributed to the remaining instances - the observer only unsubscribes when the
last instance is gone.


### Concurrent client versions

During a rolling deployment, two versions of an application can be connected at once and register the same
observer with different event types. The last registration wins: the observer follows the definition of the
client that registered last, and the earlier client stops receiving events for it.

When the client that won leaves, Chronicle hands the observer over to the most recently superseded client that
is still connected, exactly as if that client had registered again. The hand-over happens when the connection
is dropped and when the watchdog finds the client gone. It never ends a quarantine and never reactivates a
retired observer. When no earlier client remains, the observer unsubscribes.

Events of types that only the departed client handled are not replayed to the client that takes over; that
belongs to the planned union of the event types of concurrent clients. Serving both sets of event types at the
same time is not supported yet.
