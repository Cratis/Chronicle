---
title: Security-related behavior changes
description: "Review behavior changes in access checks, compliance key operations and connection handling, and learn what to do about each one when you upgrade."
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
- **Coverage is decided before the operation runs.** A grant withdrawn while an already admitted
  operation is in flight does not cancel that operation; the withdrawal applies to the next call.

## Store capabilities

- **Alert queries are decided for the addressed event store.** Reading alerts requires the
  `chronicle.alerts.read` capability at the event store the query addresses. The same decision applies
  over gRPC and over HTTP.
- **Store entitlements are audited by default.** With `EnforceStoreEntitlements=false`, a caller who lacks
  the capability on an ordinary event store is allowed, and the decision is recorded in the log. Set the option
  to `true` to require explicit event store entitlements.
- **Unknown capabilities and failed decisions are refused.** A capability that is not known, or a decision
  that cannot be made, refuses the query.

## Caller identity

- **Tokens without a subject are identified by `azp` or `client_id`.** A token that carries neither
  `sub` nor an application claim is identified by its authorized party, falling back to the client
  identifier. Connections opened by such a caller are owned by that identifier.

## Connections

- **Connection ids are reserved across the cluster.** A connection id is held by the caller that
  opened it, on every silo. The holder keeps the reservation for as long as its stream is open, and the id
  stays reserved for that owner for one hour after its last stream ends. Another caller that asks for the same id during that time receives
  `PermissionDenied`.
- **Transient reservation failures answer `Unavailable`.** When the reservation cannot be checked in
  time, the connection attempt answers `Unavailable` and the client can retry.

## Enforcement defaults

The following options on `Authentication` control enforcement. They are listed with the other
[configuration options](../hosting/configuration/authentication.md).

| Option | Default | Effect when on |
| --- | --- | --- |
| `EnforceAdministratorPolicyForOperations` | `false` | System-store operational recovery and kernel-scoped administrator-policy operations require an administrator. Application-store operations follow per-store rules. |
| `EnforceStoreEntitlements` | `false` | Store-scoped operations, including compliance operations, require the capability on each event store they reach. |
| `EnforceDestructiveOperations` | `false` | Destructive operations on the System event store or at kernel scope, and compliance key operations beyond the requested event store, require an administrator. |

This minor release preserves audit mode for unconfigured deployments. Set
`EnforceAdministratorPolicyForOperations=true` and `EnforceDestructiveOperations=true` to opt in to enforcement.
When either flag is false (including when unset), startup logs a warning naming the flag and explaining that
its default becomes enforcing in the next major release. The flags are independent: turning one off does not bypass
checks enforced by another. Administrator management and System-store access remain administrator-only
when authentication is enabled.

Legacy `DeleteEncryptionKey` and `AllowNewEncryptionKey` RPCs now return `PermissionDenied`, naming uncovered
stores, when coverage is incomplete; they never report success for a partial operation, even in audit mode.
Changes may already have been applied to covered stores. Use the coverage-returning RPCs to inspect partial results.

Before enabling `EnforceStoreEntitlements`, review the audited decisions and configure the required grants
in a test environment.
