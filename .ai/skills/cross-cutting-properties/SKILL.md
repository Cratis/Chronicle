---
name: cross-cutting-properties
description: Attach audit/correlation metadata to every appended Cratis event without polluting event types — via ICanProvideAdditionalEventInformation, plus event tags and the built-in EventContext fields. Use for correlation IDs, tenant/actor context, and other cross-cutting concerns that should travel with events but are not domain payload.
---

# Cross-Cutting Event Properties

Some information must travel with every event — correlation/causation ids, the authenticated actor, tenant context — but adding it as a property to every `[EventType]` would pollute the schemas. Chronicle solves this with `ICanProvideAdditionalEventInformation` (metadata-envelope providers) and event **tags**.

## First, check the built-in `EventContext`

Before implementing a provider, see whether what you need is already there (available in reactors/reducers):

| Property | Description |
|---|---|
| `EventSourceId` | the event source appended to |
| `SequenceNumber` | ordinal within the sequence |
| `Occurred` | wall-clock time at append |
| `CorrelationId` | propagated from the request (or generated) |
| `Causation` | upstream event references |
| `CausedBy` | actor identity (from the configured identity provider) |

**You don't need a custom provider for actor identity alone** — `CausedBy` already captures it. Reach for a provider only for *additional* fields.

## `ICanProvideAdditionalEventInformation`

```csharp
using System.Text.Json.Nodes;

public class TenantMetadataProvider(IHttpContextAccessor http) : ICanProvideAdditionalEventInformation
{
    // ProvideFor receives the event as a JsonObject and mutates it in place; it returns Task.
    public Task ProvideFor(JsonObject @event)
    {
        @event["tenantId"] = http.HttpContext?.Request.Headers["x-tenant-id"].FirstOrDefault() ?? "Default";
        return Task.CompletedTask;
    }
}
```

Chronicle discovers providers from DI — register as scoped/singleton. Multiple providers merge; key collisions = last-registered wins. Place the class at a cross-cutting infrastructure location, not inside a slice. The properties land in the event's **metadata envelope**, not the event record — they are not surfaced in `EventContext` on reactive handlers. If a value must influence a projection/reducer, it belongs on the event type (or a dedicated audit event), not in cross-cutting metadata.

## Tags vs filtering — easy to confuse

| Attribute | Where | What it does |
|---|---|---|
| `[Tag("analytics", "user-action")]` | on an `[EventType]` | merges static tags into every occurrence at append time; available in `EventContext.Tags`. Does **not** filter. |
| `[FilterEventsByTag("tag")]` | on a reactor/reducer class | restricts which events reach the handler (multiple = OR; combined with `[EventSourceType]`/`[EventStreamType]` = AND). |
| `[Tag]` / `[Tags]` | on a reactor/reducer class | admin-UI label only — **no** effect on delivery. |

Tags are also used for concurrency scoping. To *filter* by tag you need `[FilterEventsByTag]`, not `[Tag]`.

## Common pitfalls

| Pitfall | Why |
|---|---|
| Adding correlation/tenant id to every `[EventType]` | pollutes schemas — use a provider |
| Injecting scoped services into a singleton provider | register the provider scoped, or use `IServiceScopeFactory` |
| Expecting envelope properties to appear in `EventContext` on handlers | they don't — they're metadata only |
| Using a provider for data a projection needs | if the projection needs it, it belongs on the event type |

## Quality gate

- [ ] Build is clean; provider is registered in DI (not just implemented).
- [ ] No domain data hidden in cross-cutting properties — infrastructure metadata only.

## See also

- `vertical-slices.md` — event types, `EventContext` in reactors/reducers.
- `multi-tenancy` — namespace-per-tenant isolation (a different mechanism from a tenant tag).
