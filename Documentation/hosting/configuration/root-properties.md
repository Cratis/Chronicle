---
description: Root properties of chronicle.json for Chronicle Server, including the bind timeout, health check endpoint and graceful shutdown.
---
# Root Properties

These properties live at the root of `chronicle.json`.

## Example configuration

```json
{
  "port": 35000,
  "bindTimeout": "00:00:30",
  "healthCheckEndpoint": "/health",
  "maxReceiveMessageSize": 104857600,
  "shutdown": {
    "drainTimeout": "00:00:08"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| port | number | 35000 | By default, gRPC (HTTP/2) and Workbench/API/OAuth/health (HTTP/1.1) over TLS; HTTP/2-only h2c when `tls.enabled=false` |
| bindTimeout | time span | 00:00:30 | How long to retry a port bind that fails with EADDRINUSE |
| healthCheckEndpoint | string | /health | Health check endpoint path |
| maxReceiveMessageSize | number | 104857600 | The largest gRPC message in bytes the kernel accepts from a client, 100 MB by default. Remove the limit with `null` |
| maxSendMessageSize | number | unlimited | The largest gRPC message in bytes the kernel sends to a client |
| shutdown.drainTimeout | time span | 00:00:08 | Shared budget for joining local observer work and draining appended-event queues at shutdown |

## Bind timeout

`bindTimeout` defaults to 30 seconds. Chronicle retries any port bind that fails with EADDRINUSE until this timeout expires, including one blocked by a persistent listener; it delays startup failure rather than resolving a persistent conflict. Set it to `"00:00:00"` to disable retries and fail on the first bind attempt. The environment-variable form is `Cratis__Chronicle__BindTimeout=00:00:30`.

## Health check endpoint

Chronicle exposes the health check endpoint on the main port (35000). You can customize the path if your environment requires a different route.

To expose the health endpoint on a dedicated port — with TLS optionally disabled for orchestrator and load-balancer probes — see [Health Endpoint](health-endpoint.md).


## Graceful shutdown

`shutdown.drainTimeout` gives all local observers and appended-event queues one shared shutdown deadline, rather than a separate timeout per observer. Chronicle caps the budget at the host shutdown timeout minus two seconds; the server's default ten-second host timeout therefore permits at most eight seconds of draining. The effective budget is logged at startup.

Set `Cratis__Chronicle__Shutdown__DrainTimeout=00:00:05` to use a five-second budget. A zero timeout skips waiting. Leave your orchestrator enough termination grace for the host shutdown timeout.

Shutdown refuses new observer service calls, joins admitted catch-up and replay work, and flushes local bulk windows without promoting an unfinished replay. If the deadline expires, Chronicle cancels MongoDB bulk writes and logs the affected observer, partitions, and event sequence numbers when available. Cancellation means the write outcome is unknown, not that MongoDB rolled it back. Unconfirmed writes do not advance handled positions; failed partitions are recorded best-effort only while time remains.

## Message sizes

`maxReceiveMessageSize` defaults to the 100 MB the Chronicle clients allow themselves to send, so a large event, or a client-side reducer whose read model has grown, is not refused by the kernel. Raise it together with the client's `MaxSendMessageSize` if you need more. See [gRPC Message Size](../../configuration/grpc-message-size.mdx).
