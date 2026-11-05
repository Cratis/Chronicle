---
id: 0024-kernel-authorization-decision-point
title: Decide administrator-protected kernel operations through one authorization decision point
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-04
decider: woksin
applies-to:
  - Source/Kernel/Core/Security/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Decide administrator-protected kernel operations through one authorization decision point

Tracking issue: [Cratis/Chronicle#1965](https://github.com/Cratis/Chronicle/issues/1965).

## Context

Authorization in the kernel is expressed as roles. A role says who someone is, not what they may do or where. It cannot
express a scope (kernel, event store, namespace), and it cannot separate reading from changing. Three places each
asked the administrator question on their own: the administrator policy, the operations policy and the System-store routing guard.

## Decision

A hybrid model, decided on 2026-11-04 in the issue discussion, and non-breaking:

- One decision point, `IAuthorizationDecisions`, answers "does this principal hold this capability in this scope?".
- Roles stay, as bundles of capabilities. The administrator role bundles the three capabilities below.
- Enforcement points (the two policy handlers and the System-store guard) only map their input to a capability and a scope and
  act on the answer. The decision point sits above the existing administrator check and delegates to it; the existing rules
  are unchanged.
- Behaviour is unchanged in every configuration in this slice: authentication on or off, audit or enforce mode, gRPC, HTTP and in-process.

## Model

- **Capability**: a stable, machine-readable name. Never renamed.
  - `chronicle.security.administer`: users, applications and administrators.
  - `chronicle.event-store.operate`: operational mutations.
  - `chronicle.event-store.access`: reaching the System store through the routing guard.
- **Scope**: kernel, event store, or namespace of an event store. The namespace is carried but not used by any rule yet.
- **Outcome**: `Denied = 0`, `Allowed = 1`.
- **Reason**: `Failed = 0`, `Granted = 1`, `AuthenticationDisabled = 2`, `NotEnrolled = 3`, `NoPrincipal = 4`, `NotGranted = 5`, `UnknownCapability = 6`.

Values are explicit, never renumbered, and pinned by a spec. The defaults (`Denied`, `Failed`) are the safe ones.
Decisions are not exposed to clients in this slice.

## Invariants

1. Fail closed: an error, an unknown capability or a default value is a denial. For an enrolled operation while
   authentication is enabled, a missing or unauthenticated principal is a denial too. Operations that are not
   enrolled, and every operation while authentication is disabled, are allowed before the principal is checked.
   A decision that failed is never turned into an allow by audit mode.
2. No new anonymous paths: no new policy names and no attribute changes.
3. Parity across hosts: gRPC, HTTP and in-process callers all reach the same decision point.
4. No I/O of its own and no memoisation across calls, so a change of privilege is seen on the next call.
5. Denial messages, status codes and logs stay as before. Logs carry the capability name only, never claims or tokens.

## Enrolment

This slice enrols only the three existing administrator checks. Operations on application event stores are explicitly
reported as allowed with reason `NotEnrolled`, so un-enrolled traffic becomes observable.

## Rollout

The default is unchanged. A later change adds an opt-in flag first. Denying un-enrolled operations by default happens only in the
coordinated major-release window, announced beforehand.

## Performance

A decision is made per call, never per event. Before the append path is enrolled, decisions must run against a cached,
immutable grant set, with a benchmark.

## Later slices

- Separate read from mutate capabilities.
- Namespace scope.
- Scoped grants.
- Re-checks at execution time for asynchronous operations.
- Exposing outcomes to clients.

## Addendum: the exception mechanism (skeleton)

Status: pending. The default values below are not decided; they are recorded here so the mechanism is one pattern.

Where a behavior has an enforcing form that cannot be the default yet, it follows one pattern:

- **A flag.** Each is a boolean under `Authentication`, with a one-line public default constant beside it, so changing a default is a one-line change that the specs cover for both values.
- **Independent flags.** A flag governs its own scope. A broader flag that is on still enforces its broader scope.
- **Temporary opt-outs.** An opt-out exists to ease an upgrade, is documented with the release that removes it, and does not widen what any other flag covers.
- **Startup notices.** A flag whose default differs from the enforcing form may log one notice at startup stating which behavior is in effect. The notice carries no caller data.
- **Decisions are logged.** In the non-enforcing form a decision that would have been refused is logged, and in the enforcing form the refusal is logged.

| Flag | Scope | Default in code today | Final default |
| --- | --- | --- | --- |
| `EnforceAdministratorPolicyForOperations` | operational mutations | `false` (`DefaultEnforceAdministratorPolicyForOperations`) | TODO: pending decision |
| `EnforceComplianceCapabilities` | compliance operations per event store | `false` (no constant; audit-only) | TODO: pending decision |
| `EnforceDestructiveOperations` | destructive operations on the System store or at kernel scope, and compliance key operations beyond the requested event store | `false` (`DefaultEnforceDestructiveOperations`) | TODO: pending decision |

`EnforceDestructiveOperations` is deliberately narrow. Operations on an ordinary event store, such as an application replaying its own observers, stay with the per-store rules, and rotating an application secret is excluded because an administrator can rotate it again. With the flag off, such an operation is logged and allowed. A broader flag that is on still enforces its broader scope.

TODO (pending decision): the release in which each default changes, the opt-out period, the wording of the startup notices, the amendment to Invariant 4 for snapshots and caches, per-store entitlements and how legacy grants map to them, and benchmark results.

### Store capabilities enrolled so far

- `chronicle.alerts.read` is the only store capability enrolled. A query declares it with `[StoreCapability]`, and the declaration carries the capability to the event store (and namespace) the query addresses.
- `StoreCapabilityGuard` applies the decision for gRPC, and the HTTP query path applies the same decision through `StoreCapabilityDecision`, so both transports give the same answer.
- The decision goes through `IAuthorizationDecisions`. Until per-store entitlements exist, a caller that is not granted a known capability at an ordinary event store is allowed and the decision is recorded. A failed decision, an unknown capability and a decision that cannot be made are refused (fail closed).
- Users remain unenrolled: they are allowed and the decision is audited until #1965.

### Per-connection cluster reservation

- A connection id is reserved cluster-wide by a grain keyed by the connection id. The grain holds in-memory state only.
- A claim is a lease. The holder renews it while its stream is open, so a claim that is never released, such as a lost call or a holder that went away, ends by itself. A grain reactivated empty is filled by the next renewal.
- The same rules as the local ownership apply: the same owner may claim again, another owner is refused while the id is held and for a retention period after the last claim ends, and claims of silos that are no longer active are dropped.
- The connection service claims locally first and then asks the reservation within the clustering response timeout. If the reservation refuses, fails or times out, the local claim is released and the caller is refused (fail closed). The reservation is released without blocking when the stream ends.
