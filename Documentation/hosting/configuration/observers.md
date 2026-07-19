# Observers

Observer configuration controls retry behavior, timeouts, watchdog monitoring, and how events fan out to scaled-out client instances.

## Example configuration

```json
{
  "observers": {
    "subscriberTimeout": 5,
    "maxRetryAttempts": 10,
    "backoffDelay": 1,
    "exponentialBackoffDelayFactor": 2,
    "maximumBackoffDelay": 600,
    "watchdogInterval": 60,
    "fanOutStrategy": "round-robin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| subscriberTimeout | number | 5 | Timeout in seconds for observer subscriber calls |
| maxRetryAttempts | number | 10 | Maximum retry attempts for failed partitions (0 = infinite) |
| backoffDelay | number | 1 | Initial backoff delay in seconds |
| exponentialBackoffDelayFactor | number | 2 | Exponential backoff multiplier |
| maximumBackoffDelay | number | 600 | Maximum backoff delay in seconds |
| watchdogInterval | number | 60 | Interval in seconds between watchdog checks; the watchdog verifies connected clients are still active, running jobs (replay and catch-up) are still progressing, and `NextEventSequenceNumber` is up-to-date |
| fanOutStrategy | string | round-robin | Strategy for distributing events across multiple connected instances of the same client. `round-robin` distributes deterministically by partition key, keeping every partition sticky to one instance and preserving per-partition ordering. `random` picks a random instance per delivery |

## Scaled-out clients

When multiple instances of the same client application connect, its reactors and reducers all
subscribe to the same observer. Chronicle fans event delivery out across the instances using the
configured `fanOutStrategy`. If an instance disconnects, it is removed immediately and its
partitions are redistributed to the remaining instances - the observer only unsubscribes when the
last instance is gone.

