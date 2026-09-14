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
    "fanOutStrategy": "round-robin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| subscriberTimeout | number | 30 | How many seconds an observer waits for its subscriber to answer a batch before giving up on it. `0` waits indefinitely. See [Subscriber timeout](#subscriber-timeout) |
| maxRetryAttempts | number | 10 | Maximum retry attempts for failed partitions (0 = infinite) |
| quarantineOnFailedPartitionCount | number | 0 | Quarantine the observer once this many of its partitions have failed (0 = never) |
| quarantineOnFailedPartitionPercentage | number | 0.0 | Quarantine the observer once this share of its observed partitions have failed (0.0 = never) |
| backoffDelay | number | 1 | Initial backoff delay in seconds |
| exponentialBackoffDelayFactor | number | 2 | Exponential backoff multiplier |
| maximumBackoffDelay | number | 600 | Maximum backoff delay in seconds |
| definitionEvolution | string | Automatic | Controls whether projection and reducer definition changes apply `Automatic`, `PartialOnly`, or `Manual` evolution. See [Definition evolution](#definition-evolution) |
| replayOnDefinitionChange | boolean | false | Controls automatic replay for reactors and webhooks. Projection and reducer changes use `definitionEvolution` |
| watchdogInterval | number | 60 | Interval in seconds between watchdog checks; the watchdog verifies connected clients are still active, running jobs (replay and catch-up) are still progressing, and `NextEventSequenceNumber` is up-to-date |
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

## Scaled-out clients

When multiple instances of the same client application connect, its reactors and reducers all
subscribe to the same observer. Chronicle fans event delivery out across the instances using the
configured `fanOutStrategy`. If an instance disconnects, it is removed immediately and its
partitions are redistributed to the remaining instances - the observer only unsubscribes when the
last instance is gone.

