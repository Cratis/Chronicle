---
title: Health checks
description: Report whether the client holds a live connection to the Chronicle kernel through the ASP.NET Core health check pipeline.
---

An ASP.NET Core host that cannot reach the Chronicle kernel starts, stays alive, and answers
requests — while being unable to serve any of them. The Chronicle health check makes that state
visible to whatever is watching your host: a Kubernetes readiness probe, a load balancer, or a
dashboard.

## What you get

`AddCratisChronicle` registers the health check for you. There is nothing to add.

The check is registered under the name **`chronicle`**, with the tags **`chronicle`** and
**`ready`**. It reports:

- **Healthy** — the client holds a live connection to the kernel.
- **Unhealthy** — the client is not connected, or the kernel could not be reached at all. The
  exception, when there was one, is carried on the result.

The check reads the connection's state; it does not itself dial the kernel on every probe. The
client's own watchdog is what keeps reconnecting in the background.

## Exposing it

Registering a check does not expose an endpoint — you still map one. The tags let you separate
readiness from liveness, which is the distinction that matters here:

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

Chronicle tags itself `ready` and deliberately **not** for liveness. A host that cannot reach the
kernel should be taken out of rotation, but restarting it does not bring the kernel back — so a
liveness probe that failed on this would restart a healthy host in a loop while the kernel is down.

## Choosing the failure status

Report a failure as `Degraded` instead when your host can still do useful work without Chronicle:

```csharp
builder.Services.AddChronicleHealthCheck(HealthStatus.Degraded);
```

Call this before `AddCratisChronicle`, or on its own in a host that wires the client up itself.
Registration is idempotent, so the automatic registration will not add a second check.

## Startup behavior

The host starts even when the kernel is unreachable. `UseCratisChronicle` attempts a connection when
the application has started, logs a warning if it cannot get one, and leaves the client's watchdog to
keep retrying. The health check is what reports that state in the meantime — so a readiness probe
holds traffic back until the connection comes up, rather than the host failing to start at all.

## Related

- [Get started](./getting-started.md) — wiring the client into a host.
- [Observing artifact registration](./registration-outcome.md) — whether the artifacts a connected
  client declared actually registered, which is a separate question from whether it is connected.
