---
title: Cratis Chronicle .NET readiness health checks
description: Expose Chronicle connection and registration health through ASP.NET Core readiness checks so traffic waits while the client is disconnected.
---

The Cratis Chronicle ASP.NET Core health check reports whether the client is connected and its
projection artifacts registered successfully. Use it for readiness so a Kubernetes probe, load
balancer, or dashboard can see when a running host cannot yet serve work that depends on Chronicle.

## What you get

`AddCratisChronicle` registers the check automatically; [map an endpoint below](#exposing-it) to
expose it. Its name is `chronicle`, with the tags `chronicle` and `ready`. It checks the event
store configured through `ChronicleAspNetCoreOptions.EventStore`:

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

The check reads the client's connection and registration state without dialing the kernel on every
probe. The client's watchdog handles background reconnection. If your application opens other event
stores, this check does not automatically cover them.

Registration success covers projection artifacts, not confirmation that every reactor or reducer
is live or that projections have caught up with newly appended events. See
[Observing artifact registration](./registration-outcome.md#what-it-covers) for those boundaries.

## Exposing it

Map a readiness endpoint that includes checks tagged `ready`. Keep liveness separate: readiness
answers whether this host can receive traffic, while liveness answers whether it needs restarting.
This example excludes all registered checks from the liveness endpoint:

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

Chronicle tags its check `ready`, not for liveness. When the kernel is unreachable, a readiness
failure keeps traffic away from the host. Restarting the host cannot bring the kernel back, so
including this check in liveness can cause repeated restarts while the kernel is down.

## Choosing the failure status

`AddChronicleHealthCheck` takes a failure status, and you can pass `Degraded` when your host can still do useful work without Chronicle:

```csharp
builder.Services.AddChronicleHealthCheck(HealthStatus.Degraded);
```

Call this before `AddCratisChronicle`, or on its own in a host that wires the client up itself.
Registration is idempotent, so the automatic registration will not add a second check.

## Startup behavior

`UseCratisChronicle` attempts a connection after the application has started. A connection timeout
or unavailable kernel produces a warning and leaves the client's watchdog retrying; it does not
stop the host. With automatic registration enabled, readiness holds traffic back until the client
connects and registration succeeds. Other startup errors, such as client misconfiguration, are not
handled as an unavailable kernel.

After a kernel restart, readiness fails while the client is disconnected and while it re-registers.
`Registration.IsRegistering` marks that interval even though the outcome retains the previous run's
success or failure. Manual `RegisterAll` calls also make readiness fail while running, including when
automatic registration is off. Background-retry attempts set the flag too; between attempts, the
last failed outcome keeps readiness down. If application code needs to wait for the current run,
use `WaitForRegistration` to receive its new outcome instead of the previous one.

## Related

- [Get started](./getting-started.md) shows how to wire the client into a host.
- [Observing artifact registration](./registration-outcome.md) explains the outcome used by
  readiness, its failure details, and what it covers.
