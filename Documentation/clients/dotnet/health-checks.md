---
title: Health checks
description: Report whether the client is connected and its artifact registration succeeded through the ASP.NET Core health check pipeline.
---

An ASP.NET Core host that cannot reach the Chronicle kernel starts, stays alive, and answers
requests — while being unable to serve any of them. The Chronicle health check makes that state
visible to whatever is watching your host: a Kubernetes readiness probe, a load balancer, or a
dashboard.

## What you get

`AddCratisChronicle` registers the health check for you. There is nothing to add.

The check is registered under the name **`chronicle`**, with the tags **`chronicle`** and
**`ready`**. It checks the event store configured through `ChronicleAspNetCoreOptions.EventStore`:

| State | Result |
| --- | --- |
| Not connected, or the kernel cannot be reached | Failure status (Unhealthy by default) |
| Connected, but artifact registration has not completed | Failure status |
| Connected, but `Registration.IsRegistering` is `true`, even after an earlier success | Failure status |
| Connected, but registration stopped with an exception or any declared projection artifact failed | Failure status |
| Connected, `Registration.IsSuccess` is `true`, and `IsRegistering` is `false` | Healthy |
| Connected, `AutoDiscoverAndRegister` is off, and the application has not run `RegisterAll` | Healthy |

A registration failure carries the run's exception on the result, when there was one, and the
failed artifact type names in `Data["failedArtifacts"]`. Partial registration is not ready, even
though the artifacts that succeeded may already serve requests. Identity drift findings alone do
not make successful registration unhealthy.

The check reads the connection and registration state; it does not itself dial the kernel on every
probe. The client's own watchdog is what keeps reconnecting in the background. Other event stores
your application opens are not checked automatically.

Registration success covers projection artifacts, not confirmation that every reactor or reducer
is live or that projections have caught up with newly appended events. See
[Observing artifact registration](./registration-outcome.md#what-it-covers) for those boundaries.

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

`AddChronicleHealthCheck` takes a failure status, and you can pass `Degraded` when your host can still do useful work without Chronicle:

```csharp
builder.Services.AddChronicleHealthCheck(HealthStatus.Degraded);
```

Call this before `AddCratisChronicle`, or on its own in a host that wires the client up itself.
Registration is idempotent, so the automatic registration will not add a second check.

## Startup behavior

The host starts even when the kernel is unreachable. `UseCratisChronicle` attempts a connection when
the application has started, logs a warning if it cannot get one, and leaves the client's watchdog to
keep retrying. The health check is what reports that state in the meantime — so a readiness probe
holds traffic back until the connection comes up and artifact registration succeeds, rather than
the host failing to start at all. After a kernel restart, readiness drops while the client is
disconnected and stays down while it re-registers after reconnecting. `Registration.IsRegistering`
marks that interval even though the outcome retains the previous run's success or failure. A manual
`RegisterAll` also makes readiness fail while it is running, including when automatic registration
is off. Background-retry attempts set `IsRegistering` too; between attempts, readiness still fails
because the last registration outcome is a failure. `WaitForRegistration` waits for the new run's outcome rather than returning the previous one.

## Related

- [Get started](./getting-started.md) — wiring the client into a host.
- [Observing artifact registration](./registration-outcome.md) — the registration outcome used by
  readiness, its failure details, and what it covers.
