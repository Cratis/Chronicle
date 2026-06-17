---
name: multi-tenancy
description: Isolate tenants in a Cratis application with Chronicle namespaces — Arc maps the current tenant to a namespace (TenantNamespaceResolver), and each namespace has its own events, projections, reducers, and read models. Use when one deployment must serve multiple tenants with data isolation. Keep it product-neutral — not every Cratis app is multi-tenant.
---

# Multi-Tenancy with Chronicle Namespaces

Chronicle implements multi-tenancy through **namespaces**: each namespace is a logically separate event store. Events, projections, reducers, and observers run independently per namespace — there is no cross-namespace leakage. **Not every Cratis app is multi-tenant** — adopt this only when one deployment must serve multiple tenants with isolation.

## Core concept

| Term | Meaning |
|---|---|
| Namespace | a named isolation boundary in Chronicle (its own event store) |
| Default namespace | `"Default"` — used when none is resolved |

All appends, projections, and observers are scoped to the resolved namespace. A reactor for `<Entity>Created` fires once per tenant namespace that has that event — independently.

## Arc tenancy integration

When using Arc with Chronicle, tenancy maps to namespaces automatically: Arc's `TenantNamespaceResolver` maps the current tenant id to the Chronicle namespace and falls back to the default namespace when no tenant is set. Enable Arc tenancy in startup so the tenant context resolves before each command/query handler runs; namespace wiring is then automatic — no manual resolver registration needed.

If you need custom namespace resolution outside Arc tenancy (header, subdomain, JWT claim), Chronicle supports namespace resolvers registered in priority order; the first non-null result wins, else the default namespace is used.

## Observer and reactor isolation

Observers (projections, reducers, reactors) are instantiated **per namespace**. Consequences:

- A `[OnceOnly]` reactor fires once **per event source within each namespace** (i.e. once per tenant), not globally once — see reactors.md.
- Projection rewind affects only the target namespace.
- Each namespace has its own sequence numbers.

## Common pitfalls

| Pitfall | Why |
|---|---|
| Storing a tenant id on every event type | the namespace *is* the tenant — events don't need a tenant property |
| No tenant resolution in a multi-tenant deployment | every tenant lands in `"Default"` — no isolation |
| Reading one namespace and writing another in the same request | accidental cross-namespace access is a bug (intentional bridging is a Translation reactor) |
| Expecting `[OnceOnly]` to be globally once | it is per-namespace |

## Quality gate

- [ ] Build is clean.
- [ ] Tenant resolution is configured and resolves the expected namespace from test requests.
- [ ] No tenant identifier appears on `[EventType]` records.
- [ ] `[OnceOnly]` reactors are understood to fire per-namespace.

## See also

- `cross-cutting-properties` — injecting tenant metadata into event envelopes (distinct from namespace isolation).
- `auth-and-identity` — resolving the current tenant/user.
