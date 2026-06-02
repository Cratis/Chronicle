# Observers

Observer configuration controls retry behavior, timeouts, and watchdog monitoring for observer subscribers.

## Example configuration

```json
{
  "observers": {
    "subscriberTimeout": 5,
    "maxRetryAttempts": 10,
    "backoffDelay": 1,
    "exponentialBackoffDelayFactor": 2,
    "maximumBackoffDelay": 600,
    "watchdogInterval": 60
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

