---
title: Cratis Chronicle configuration best practices
description: Keep Chronicle deployments repeatable with pinned images, read-only configuration, environment-specific secrets, and workload-based observer settings.
---
# Best Practices

For repeatable Chronicle Kernel deployments, pin the server image version, mount shared configuration read-only, and supply credentials at deployment time. Keep environment-specific values outside the shared file so a storage connection or secret does not travel with it into another environment.

Use `chronicle.json` for shared settings and environment variables for overrides. A matching environment variable wins over the file; [Configuration Precedence](configuration-precedence.md) explains how the Kernel combines these sources.

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
| observers.subscriberTimeout | Allow enough time for subscriber work at the latency of your infrastructure |
| observers.maxRetryAttempts | Set a finite retry limit, or use `0` for unlimited retries |
| events.queues | Spread the routing of appended events when many observers share an event sequence; see [How observers use the queues](events.md#how-observers-use-the-queues) |

The example is a starting point, not a workload recommendation. Choose the subscriber timeout and retry limit according to how long your subscribers need to complete their work and how you intend to recover from failures. See [Observers](observers.md) before changing these values.

Increasing `events.queues` spreads live appended-event routing when observers share an event sequence. It does not control replay or catch-up, which read from the event log. Use [Events](events.md) to decide whether changing the queue count addresses your workload.

## Prepare the deployment

Use a specific image version rather than `latest`, and mount `chronicle.json` read-only. Keep the image version and required environment variables with the deployment manifests so another operator can identify what is running.

Supply environment-specific connection strings through your platform's secret mechanism rather than committing credentials to JSON. [Docker Configuration](docker.md) shows the file mount and environment-variable forms; [Production hosting](../production.md) covers the certificates and other requirements for starting the production image.

If schema provisioning runs separately from the Kernel, follow [SQL schema operations](sql-schema-operations.md) or [MongoDB index operations](mongodb-index-operations.md) before reducing runtime permissions. Each guide explains what verification checks and which operations can still create schema objects at runtime.

