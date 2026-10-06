<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/observable-emission-guards.md -->
# Observable query emission guards

Verified against `Cratis.Arc.Core` `22.41.1` (`Cratis.Arc.Queries`).

Authorization for an observable query runs **once, when the subscription is
established**. It decides whether the caller may obtain the live stream. It does
**not** end the stream when a token expires, a session is signed out, or a role or
membership changes; keep-alive pings keep the connection open and the client
reconnects on its own. An **emission guard** closes that gap.

## The opt-in

Implement `IGuardObservableQueryEmission`. Guards are discovered by convention:
no registration, no configuration. With no guard installed, emissions take their
existing path.

```csharp
public class SessionMustStillBeActive(ISessions sessions) : IGuardObservableQueryEmission
{
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        var sessionId = context.Principal?.FindFirst("sid")?.Value;
        if (sessionId is null) return ObservableQueryEmissionVerdict.DenyAndTerminate;

        return await sessions.IsActive(sessionId)
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.DenyAndTerminate;
    }
}
```

`ISessions` is the application's own store of authoritative session state — not an
Arc API, and never an always-true stub.

It runs on **every emission** of **every subscription**, on **every transport**:
the multiplexed hub, WebSocket and SSE.

## Verdicts

| Verdict | Effect |
| --- | --- |
| `Allow` | The emission is written unchanged. |
| `Suppress` | This emission is withheld; the subscription stays live. It does **not** move the delta baseline, so the next `ChangeSet` is computed against what the client last actually received. |
| `DenyAndTerminate` | Nothing is written and **that subscription only** is torn down (siblings on the same connection keep streaming). The client gets `Unauthorized`, latches the denial and stops reconnecting. |

Several guards: every guard is asked, the **most restrictive** verdict wins
(`DenyAndTerminate` over `Suppress` over `Allow`), and the first
`DenyAndTerminate` short-circuits the rest.

## The context

`ObservableQueryEmissionContext` carries `QueryName`, `Arguments`,
`SubscriptionScope`, `Principal`, `CorrelationId`, `IsFirstEmission`,
`CancellationToken` and the `ServiceProvider` to resolve from. Each guard gets its
own copy of the scope and arguments.

- The principal is passed **explicitly** in `Principal`. Emissions arrive on the
  producer's thread where the request's ambient context does not flow, so do not
  use ambient accessors, and on **hub** emissions do not use `IHttpContextAccessor`:
  there is no native `HttpContext` after admission.
- On a **WebSocket** the identity is frozen at the handshake and never refreshes.
  The guard must look it up against your source of truth (session store, revocation
  list, token introspection), not re-read it.
- `QueryContext.SubscriptionScope` lets a **query filter** record what it decided
  (for example the organization). Arc captures it after the filters succeed and
  **before** the query executes, and hands each emission a fresh copy; a guard can
  compare it to current membership. It must serialize with Arc's
  `JsonSerializerOptions` (small, primitive values; no live services or caches),
  or the query fails as `InvalidSubscriptionScope`. Setting it does not filter the
  data: the query must select its data with the same value. **Controller-based**
  observable queries do not run `IQueryFilters`, so their guards always receive
  `null`.

## Fail closed, and its blast radius

A guard that **throws** is treated as `DenyAndTerminate` and logged at `Error`.
The aggregator is a process-wide singleton over every discovered guard type, so a
guard that **cannot be constructed** (an open generic, or a constructor the
subscription scope cannot resolve) denies **every** observable query in the
process on every transport, with only an `Error` log as the symptom. Cover a new
guard with a spec that runs its real constructor. A cancellation of the
subscription's own token while a guard runs ends the subscription without logging
a failure.

## Cost

- The guard is **constructed per emission** and never disposed, because Arc's
  `IFoo → Foo` convention does not match a guard named for what it decides. Keep
  the constructor trivial and put caches in an injected **singleton**, not in a
  field on the guard.
- Prefer cached state, a local revocation list or a short-TTL lookup to a network
  round trip per emission.
- Constructor dependencies come from the subscription's own scope, so a scoped
  session store is safe to inject.

## What a guard does not protect

- The HTTP **snapshot** path (`ObservableQueryHttp`, including
  `waitForFirstResult=true`) is not an emission guard path. Apply subscription-time
  authorization and safe data selection to every query.
- A guard is not a substitute for the subscription-time declaration: the
  `[Authorize]`/`[Roles]`/policy on the read model still decides who may subscribe.
- Interceptor masking is not an access control; the producer must never yield
  rows or fields the caller may not receive.
