---
name: cratis-chronicle-multi-tenancy
description: Isolate tenants in Chronicle with namespaces - what a namespace actually isolates, how a namespace is selected and resolved through IEventStoreNamespaceResolver, how namespaces come into existence, and the code rules that let a single-tenant deployment grow a second tenant. Use when setting up tenancy, when writing code that touches events or data on behalf of a tenant, or when adding a second tenant. Do not use for authentication, authorization, or identity-provider setup.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-multi-tenancy/SKILL.md -->

# Multi-tenancy with Chronicle namespaces

Chronicle implements multi-tenancy through **namespaces**. A namespace is a
logically separate store: its events, observers, and read models run
independently, and there is no cross-namespace leakage.

**Assume every application is multi-tenant.** A deployment serving one
organization today is a multi-tenant application with one tenant in it. The
second tenant arrives later, and code written as if there would only ever be one
is discovered from the far side of a data migration. Whether a deployment
*configures* tenant resolution is an operational choice; whether the code
survives a second tenant is not.

## Verified product sources

This skill is verified against this exact source:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.2` | `EventStoreNamespaceName`, `IEventStoreNamespaceResolver`, `IChronicleClient.GetEventStore` |
| `Cratis.Chronicle.AspNetCore` | `16.45.2` | the header and subdomain namespace resolvers |

Reverify product sources before claiming support for another version.

## The concept

| Term | Meaning |
| --- | --- |
| Namespace | a named isolation boundary within an event store |
| Default namespace | `EventStoreNamespaceName.Default`, whose value is literally `"Default"` |

`Cratis.Chronicle.EventStoreNamespaceName` is a `ConceptAs<string>` with
implicit conversion from `string`, a `Default` value of `"Default"`, and a
`NotSet` value.

## What a namespace actually isolates

Verified against the storage naming and the grain keys, not inferred:

**Per namespace:**

- event sequences, and therefore **sequence numbers**;
- observers — projections, reducers, and reactors — with their subscription
  state, failed partitions, handled counts, and replay contexts;
- read models and their sinks;
- jobs and job steps;
- constraint state, including unique-value indexes and closed streams;
- **encryption keys**, and therefore the scope of a personal-data erasure;
- identities and changesets.

**Shared across namespaces, per event store:**

- the namespace registry itself;
- observer *definitions* and other event-store-wide metadata.

The practical consequences:

- A reactor for a given event fires **once per namespace** that has that event —
  independently, not once globally.
- A replay affects only the target namespace.
- Each namespace has its own sequence numbers, so a sequence number is only
  meaningful together with its namespace.
- Two tenants can hold the same unique value without violating a uniqueness
  constraint.
- **An erasure request that spans tenants is one call per namespace.** Completing
  only some of them is an incomplete erasure.

> One asymmetry worth knowing when reading a database directly: read models for
> the **default** namespace live in the unsuffixed event-store database, while
> every other namespace gets a suffixed one.

## Selecting the namespace

There is exactly one way to get an event store, and the namespace is its second
argument:

```csharp
Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default);
```

When the namespace is omitted, the registered **namespace resolver** supplies it.
`IEventStore` exposes the namespace it was resolved for, and
`IEventStore.GetNamespaces(...)` lists them.

## Resolving the namespace

The contract is `Cratis.Chronicle.IEventStoreNamespaceResolver`, with a single
synchronous member:

```csharp
public interface IEventStoreNamespaceResolver
{
    EventStoreNamespaceName Resolve();
}
```

There is **no `ICanResolveNamespace` and no `INamespaceResolver`** — do not write
either name.

Chronicle ships four implementations:

| Resolver | Namespace | Behavior |
| --- | --- | --- |
| `DefaultEventStoreNamespaceResolver` | `Cratis.Chronicle` | always `Default` |
| `ClaimsBasedNamespaceResolver` | `Cratis.Chronicle` | a claim, `tenant_id` by default; falls back to `Default` |
| `HttpHeaderEventStoreNamespaceResolver` | `Cratis.Chronicle.AspNetCore.Namespaces` | a header, `x-cratis-tenant-id` by default |
| `SubdomainNamespaceResolver` | `Cratis.Chronicle.AspNetCore.Namespaces` | the host subdomain |

**Registration selects exactly one winner — it is not a priority chain.** The
order is: a resolver set on the Chronicle builder, then the resolver type named
in the client options, then `DefaultEventStoreNamespaceResolver`. Writing a
second resolver does not add a fallback; it is simply not used.

A custom resolver is a small class implementing the interface:

```csharp
using Cratis.Chronicle;

public class <ResolverName>(<IDependency> <dependency>) : IEventStoreNamespaceResolver
{
    public EventStoreNamespaceName Resolve() =>
        <dependency>.<CurrentTenant> is { } tenant
            ? new EventStoreNamespaceName(tenant.ToString())
            : EventStoreNamespaceName.Default;
}
```

The resolver is registered as a **singleton**, while the resolved `IEventStore`
is **scoped** — so per-request resolution works through the scope, and the
resolver itself must not cache a tenant.

> **`TenantNamespaceResolver` is not a Chronicle type.** It appears in Chronicle's
> documentation as an example resolver you write yourself. If a Cratis
> integration package supplies an automatic tenant-to-namespace resolver, confirm
> that in the owning package's source before relying on it; do not assume it
> exists.

## Namespaces come into existence on demand

A namespace does not have to be declared. Activating an event sequence for a
`(store, namespace)` pair registers the namespace, and that registration is
idempotent. There is also an explicit "ensure namespace" operation on the
server's API — but the .NET client surfaces only the read side.

**Namespace lookup is case-insensitive, while the physical database name is
byte-exact.** Two spellings register once, under the first casing seen. Normalize
the tenant identifier in the resolver rather than relying on that.

## Code rules that let a second tenant arrive safely

- **No tenant identifier on an event type.** The namespace *is* the tenant; an
  event does not need a tenant property, and adding one creates a second source
  of truth that can disagree with the namespace.
- **No singleton holds tenant-scoped state.** A singleton that captures an event
  store, a collection, or a database context pins itself to whatever namespace
  the root scope resolved — and it then returns empty results rather than
  failing, which is far harder to notice.
- **No process-wide or static cache of tenant data.** If a cache exists, the
  tenant is part of its key.
- **No hard-coded tenant identifier**, and every background flow states which
  tenant it acts for. A background job has no request to resolve from, so it must
  pass the namespace explicitly.
- **Never read one namespace and write another in the same operation.**
  Accidental cross-namespace access is a bug. Deliberate bridging between
  namespaces is a translation reactor, modeled as such.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Storing a tenant id on every event | the namespace is already the tenant; the property can drift out of agreement with it |
| No resolver configured in a multi-tenant deployment | every tenant lands in `Default` and there is no isolation |
| A singleton holding an event store, a collection, or a context | pinned to the default namespace forever, and it returns empty rather than throwing |
| Registering a second resolver expecting a fallback | one resolver wins; the other is never called |
| Expecting a once-only reactor to fire once globally | observers run per namespace, so it fires once per tenant |
| Comparing sequence numbers across namespaces | they are independent sequences |
| Erasing personal data in one namespace and calling it done | erasure is per namespace |
| Relying on namespace casing | lookup is case-insensitive, storage naming is not |

## Verify

- Tenant resolution is configured and resolves the expected namespace from a
  representative request.
- Exactly one namespace resolver is registered, and it is the intended one.
- No `[EventType]` record carries a tenant identifier.
- No singleton holds an event store, a collection, a database context, or any
  other tenant-scoped value.
- Every cache key includes the tenant.
- Every background flow states its namespace explicitly.
- Reactor and job behavior is understood as per-namespace.
- The build is clean and the specifications pass against the verified package
  version.
