---
title: Enforce a unique value in Cratis Chronicle
description: Use a Chronicle uniqueness constraint to reject competing claims for an email, ISBN or username at append time, then handle the violation.
---

Use a uniqueness constraint in Cratis Chronicle when different event sources must not claim the same value, such as two members registering the same email address. The kernel checks the rule during append and rejects a competing claim before committing that event.

## Why a constraint, not a read-model check

A query can tell the user that an email looks available, but it cannot reserve it. Two callers can both read an available value before either appends, and [eventually consistent read models](../read-models/) can lag behind committed events. Put the rule in a [constraint](../constraints/) so each writer's append is checked against the kernel's index of claimed values.

## Do it

1. Choose a [model-bound constraint](../constraints/model-bound/unique.mdx) when the rule belongs on the event's properties, or a [declarative constraint](../constraints/declarative/unique.mdx) when you want a separate definition. Follow that guide for your client's declaration syntax and setup.
2. Include every event type that can claim the value. For an email address, registration and address changes must share the rule even if their properties have different names.
3. Decide when a value becomes available again. The default mode keeps one current value per event source; claiming a new value frees the previous one. If the source must reserve every value it has used, choose the declarative guide's `PerValue` mode. Declare removal events when the application should release claims.
4. Append events through your normal command flow and handle a constraint violation as a rejected request. For an email conflict, surface it to the user as "that email is already registered."
5. Verify the rule with two different event sources: let the first claim a value, then confirm that the second receives a constraint violation for the same value. Reusing the value on its owning source is allowed, so it is not a duplicate-claim check. [Test a slice](./test-a-slice) shows how to check append results in-process.

:::note
A constraint checks claims within one namespace and one event sequence; it does not enforce uniqueness across either boundary. If each tenant has a [namespace](../concepts/namespaces.md), the same value can be claimed independently in each tenant's namespace.
:::

The event write and the claim-index update are not crash-atomic. If an index update cannot complete, the kernel refuses governed appends with `ConstraintIndexUnavailable` while the index catches up. Treat that as an unavailable check, not as proof that the value is already taken. See [constraint enforcement limits](../constraints/#constraint-or-decision-read) before relying on the rule.

## See also

- [Constraints](../constraints/) — the full constraint model and how violations are reported.
- [Namespaces](../concepts/namespaces.md) — how isolation affects "unique".
