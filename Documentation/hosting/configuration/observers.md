# Observers

Observer configuration controls retry behavior, timeouts, watchdog monitoring, and how events fan out to scaled-out client instances.

## Example configuration

```json
{
  "observers": {
    "maxRetryAttempts": 10,
    "backoffDelay": 1,
    "exponentialBackoffDelayFactor": 2,
    "maximumBackoffDelay": 600,
    "quarantineOnFailedPartitionCount": 0,
    "quarantineOnFailedPartitionPercentage": 0.0,
    "watchdogInterval": 60,
    "fanOutStrategy": "round-robin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| subscriberTimeout | number | 5 | Reserved. Not currently applied to subscriber calls — setting it has no effect |
| maxRetryAttempts | number | 10 | Maximum retry attempts for failed partitions (0 = infinite) |
| quarantineOnFailedPartitionCount | number | 0 | Quarantine the observer once this many of its partitions have failed (0 = never) |
| quarantineOnFailedPartitionPercentage | number | 0.0 | Quarantine the observer once this share of its observed partitions have failed (0.0 = never) |
| backoffDelay | number | 1 | Initial backoff delay in seconds |
| exponentialBackoffDelayFactor | number | 2 | Exponential backoff multiplier |
| maximumBackoffDelay | number | 600 | Maximum backoff delay in seconds |
| watchdogInterval | number | 60 | Interval in seconds between watchdog checks; the watchdog verifies connected clients are still active, running jobs (replay and catch-up) are still progressing, and `NextEventSequenceNumber` is up-to-date |
| fanOutStrategy | string | round-robin | Strategy for distributing events across multiple connected instances of the same client. `round-robin` distributes deterministically by partition key, keeping every partition sticky to one instance and preserving per-partition ordering. `random` picks a random instance per delivery |

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

