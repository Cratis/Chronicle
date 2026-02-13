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
| events.queues | Scale queues based on expected event throughput |

1. Use specific version tags instead of `latest` for production deployments.
2. Mount configuration as read-only to prevent accidental modifications.
3. Use environment-specific connection strings for MongoDB.
4. Configure appropriate timeouts based on your infrastructure.
5. Use environment variables for sensitive configuration like connection strings.
6. Use secrets management for production environments.
7. Set appropriate observer retry policies based on reliability requirements.
8. Configure event queues based on event throughput needs.

