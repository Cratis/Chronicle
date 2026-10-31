---
description: Best practices for configuring Chronicle Server in production, such as pinning image version tags and mounting configuration as read-only.
---
# Best Practices

## Example configuration

```json
{
  "observers": {
    "subscriberTimeout": 5,
    "maxRetryAttempts": 10
  },
  "events": {
    "queues": 8
  }
}
```

| Property | Description |
| --- | --- |
| observers.subscriberTimeout | Keep observer timeouts aligned with infrastructure latency |
| observers.maxRetryAttempts | Tune retries to balance resilience and recovery time |
| events.queues | Spread the routing of appended events when many observers share an event sequence; see [How observers use the queues](events.md#how-observers-use-the-queues) |

1. Use specific version tags instead of `latest` for production deployments.
2. Mount configuration as read-only to prevent accidental modifications.
3. Use environment-specific connection strings for MongoDB.
4. Configure appropriate timeouts based on your infrastructure.
5. Use environment variables for sensitive configuration like connection strings.
6. Use secrets management for production environments.
7. Set appropriate observer retry policies based on reliability requirements.
8. Configure event queues based on how many observers share an event sequence.

