---
title: Enforce a unique value
description: Reject an append when a value (email, ISBN, username) would duplicate an existing one.
---

**Goal:** a value must be unique — no two members with the same email, no two books with the same ISBN — and you want the system to *reject* an append that would break that, rather than discover the duplicate later.

## Why a constraint, not a read-model check

You might be tempted to query a read model first to check for duplicates. Don't rely on that: read models are [eventually consistent](../read-models/), so two appends racing at the same time could both pass the check. A [constraint](../constraints/) is enforced by the kernel **at append time**, against the authoritative state — it closes that race.

## Do it

1. Declare a **uniqueness constraint** for the value on the relevant event (see [Constraints](../constraints/) for the declarative and model-bound options).
2. Append events as normal. When an append would violate the constraint, the kernel **rejects it** and the caller gets a failure result instead of a committed event.
3. Handle the rejection in your command flow — surface it to the user as "that email is already registered."

:::note
Uniqueness is scoped. Within a multi-tenant system, [namespaces](../concepts/namespaces.md) isolate data per tenant, so "unique" means unique *within that tenant's namespace* unless you design otherwise.
:::

## See also

- [Constraints](../constraints/) — the full constraint model and how violations are reported.
- [Namespaces](../concepts/namespaces.md) — how isolation affects "unique".
