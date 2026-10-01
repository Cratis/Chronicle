# Observers

Observer configuration controls retry behavior, timeouts, watchdog monitoring, and how events fan out to scaled-out client instances.

## Example configuration

```json
{
  "observers": {
    "subscriberTimeout": 30,
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
| subscriberTimeout | number | 30 | How many seconds an observer waits for its subscriber to answer a batch before giving up on it. `0` waits indefinitely. See [Subscriber timeout](#subscriber-timeout) |
| maxRetryAttempts | number | 10 | Maximum retry attempts for failed partitions (0 = infinite). The limit applies per retry budget: clearing the quarantine of a partition starts a new budget, so it gets this many attempts again |
| quarantineOnFailedPartitionCount | number | 0 | Quarantine the observer once this many of its partitions have failed (0 = never) |
| quarantineOnFailedPartitionPercentage | number | 0.0 | Quarantine the observer once this share of its observed partitions have failed (0.0 = never) |
| backoffDelay | number | 1 | Initial backoff delay in seconds |
| exponentialBackoffDelayFactor | number | 2 | Exponential backoff multiplier |
| maximumBackoffDelay | number | 600 | Maximum backoff delay in seconds |
| definitionEvolution | string | Automatic | Controls whether projection and reducer definition changes apply `Automatic`, `PartialOnly`, or `Manual` evolution. See [Definition evolution](#definition-evolution) |
| replayOnDefinitionChange | boolean | false | Controls automatic replay for reactors and webhooks. Projection and reducer changes use `definitionEvolution` |
| watchdogInterval | number | 60 | Interval in seconds between watchdog checks; the watchdog verifies connected clients are still active, running jobs (replay and catch-up) are still progressing, and `NextEventSequenceNumber` is up-to-date |
| maxConcurrentPartitions | number | 32 | Upper bound on how many job steps (replay and catch-up work) run in parallel. The effective limit is the smaller of this value and `jobs.maxParallelSteps`. Despite the name it does not limit live event delivery. See [Job throttling](job-throttling.md) |
| fanOutStrategy | string | round-robin | Strategy for distributing events across multiple connected instances of the same client. `round-robin` distributes deterministically by partition key, keeping every partition sticky to one instance and preserving per-partition ordering. `random` picks a random instance per delivery |

## Definition evolution

Chronicle compares every newly registered projection and reducer definition with its stored definition. It then chooses the minimum operation it can prove correct:

| Classification | Behavior |
| --- | --- |
| No action | A newly consumed event type has no historical events, so existing read models cannot change |
| Partial replay | A projection adds independently mapped event types, or a reducer adds event types that occur only after its previously consumed events for each affected event source. Chronicle applies only those new event types to the affected event sources |
| Full replay | The mapping, reducer implementation fingerprint, filter, key, join, child structure, or another order-dependent part changed. Chronicle rebuilds the complete observer |

`Automatic` applies every classification. `PartialOnly` applies no-action and partial plans, but creates a replay recommendation when a full replay is required. `Manual` creates a recommendation for both partial and full plans. No-action plans never create recommendations because history cannot be affected.

Chronicle deliberately falls back to full replay when it cannot prove that event sources and mapped properties are independent. Auto-mapping, overlapping property mappings, joins, parent/child relationships, custom keys, removals, and subscriptions to every event can combine historical contributions and therefore cannot use partial replay safely.

Reducer registrations include a fingerprint of their reducer methods. Changing reducer code therefore triggers classification even when its event-type subscription is unchanged. Reducer methods are imperative, so Chronicle only chooses a partial reducer replay when history proves every newly consumed event follows all previously consumed events within its event source. Interleaved history falls back to full replay.

## Subscriber timeout

Every path that hands events to an observer's subscriber — live delivery, catch-up and replay — waits at most
`subscriberTimeout` seconds for an answer. When it elapses, the batch is recorded as a failed partition of kind
`Timeout` and the partition retries with the usual backoff.

Two things are worth knowing before changing it:

- **Giving up abandons the wait, not the work.** The subscriber is a grain and keeps processing the batch it was
  handed; the events are simply redelivered when the partition retries. Observers are expected to be idempotent, so
  that is safe — but it does mean a short timeout can have a subscriber working on a batch the kernel has already
  written off.
- **Raising it past the transport's own response timeout has no effect**, because that one gives up first. The
  default deliberately matches it, so the setting bounds nothing new until you lower it. Lowering it is the point:
  a subscriber that should always answer in a second or two gets its partition back into retry quickly instead of
  holding a job step for half a minute.

Set it to `0` to wait indefinitely — the escape hatch for a subscriber whose work legitimately has no upper bound.

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

## Ending quarantine

An observer's quarantine ends only through one of these actions:

1. An operator clears it with `cratis chronicle observers clear-quarantine`, from the Workbench, or through
   the corresponding clear-quarantine API.
2. The observer receives an explicit fresh subscription through `Subscribe` or `SubscribeToAllEvents`.
   Automatic event-store-subscription reconciliation does not end quarantine.

What can establish a fresh subscription depends on the observer:

| Observer | Subscribed again |
| --- | --- |
| Reactors and reducers of an application | When an instance of the application's client connects or reconnects, including after a Kernel restart, and when the client registers a changed definition |
| Projections | When the Kernel starts, when a client registers a changed definition, and when a namespace is added |
| Webhooks | When the Kernel starts, when a client registers a new or changed webhook, when a webhook is added or edited (target URL, headers, authorization or event types), and when a namespace is added |
| Event store subscriptions | Use `ClearObserverQuarantine` through the CLI, Workbench or API to end quarantine. Automatic reconciliation establishes the subscription and updates its event types without ending quarantine |
| The Kernel's own reactors and pattern capture | When the Kernel starts, when an event store is added, and when a namespace that already holds events is added. Pattern capture is also subscribed again when a client registers new event types |

These actions do **not** end quarantine:

- Watchdog ticks, including checks for missing jobs and stranded catch-up preparation.
- Catch-up or replay completion, including completion for individual partitions.
- Unsubscription or observer grain deactivation and reactivation.
- Automatic event-store-subscription reconciliation, whether triggered by startup, manager reactivation,
  source availability, namespace notifications, definition reconciliation, or the minute check.

A Kernel restart ends quarantine only for observers that receive a fresh subscription as listed above;
event store subscriptions remain quarantined until an operator clears them.

Completing catch-up or replay still persists progress and clears completed-work markers without resuming the
observer. Clearing quarantine then re-evaluates remaining work from the recorded position. If the observer
has no subscription, clearing leaves it disconnected until a subscription is established. Quarantine does
not cancel already-running catch-up or replay jobs, and partition completion can still start required
partition continuation work without ending the observer's quarantine.

Clearing observer quarantine does not clear failed partitions or their separate quarantine status.

## Scaled-out clients

When multiple instances of the same client application connect, its reactors and reducers all
subscribe to the same observer. Chronicle fans event delivery out across the instances using the
configured `fanOutStrategy`. If an instance disconnects, it is removed immediately and its
partitions are redistributed to the remaining instances - the observer only unsubscribes when the
last instance is gone.

