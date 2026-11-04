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

1. Fail closed: an error, an unknown capability, a missing or unauthenticated principal, or a default value is a denial.
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
