---
title: Security-related behavior changes
description: Behavior changes in access checks, compliance key operations and connection handling, and what to do about each.
---

This page lists behavior that differs from earlier releases in how Chronicle checks callers and
reserves connections. None of the changes alter a wire contract or a grain interface that existed before.

## Compliance key operations

- **The System event store is not part of the fan-out.** Key deletion and allowing a new key apply to
  the requested event store and to the other application event stores the caller may operate on. The
  System event store is left out.
- **A listing failure answers `Unavailable`.** When the other event stores cannot be listed, the call
  is not carried out for a partial set. Retry the call.
- **A denied requested store answers `PermissionDenied` alone.** The answer does not include the list
  of other event stores. Callers allowed on the requested store still receive the list of stores that
  were not covered.

## Store capabilities

- **Alert queries are decided for the addressed event store.** Reading alerts requires the
  `chronicle.alerts.read` capability at the event store the query addresses. The same decision applies
  over gRPC and over HTTP.
- **Users are not enrolled yet.** A user who has not been granted the capability on an ordinary event store
  is allowed, and the decision is recorded in the log.
- **Unknown capabilities and failed decisions are refused.** A capability that is not known, or a decision
  that cannot be made, refuses the query.

## Caller identity

- **Tokens without a subject are identified by `azp` or `client_id`.** A token that carries neither
  `sub` nor an application claim is identified by its authorized party, falling back to the client
  identifier. Connections opened by such a caller are owned by that identifier.

## Connections

- **Connection ids are reserved across the cluster.** A connection id is held by the caller that
  opened it, on every silo. The holder keeps the reservation for as long as its stream is open, and it is
  released shortly after the stream ends. Another caller that asks for the same id during that time receives
  `PermissionDenied`.
- **Transient reservation failures answer `Unavailable`.** When the reservation cannot be checked in
  time, the connection attempt answers `Unavailable` and the client can retry.

## Optional enforcement

The following options on `Authentication` are off by default. They are listed with the other
[configuration options](../configuration/chronicle-options.mdx#authentication).

| Option | Effect when on |
| --- | --- |
| `EnforceAdministratorPolicyForOperations` | Operational mutations require an administrator. |
| `EnforceComplianceCapabilities` | Compliance operations require the compliance capability on every event store they reach. |
| `EnforceDestructiveOperations` | Destructive operations on the System event store, and compliance key operations beyond the requested event store, require an administrator. |

Enable an option in a test environment first, and watch the decision messages in the log before
enabling it where it matters.
