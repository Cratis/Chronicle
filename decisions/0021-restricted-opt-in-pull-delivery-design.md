---
id: 0021-restricted-opt-in-pull-delivery-design
title: Explore restricted opt-in pull delivery without admitting a receiver API
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-29
applies-to:
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Grpc/Observation/**
  - Source/Kernel/Contracts/Observation/**
  - Source/Kernel/Server/**
  - Documentation/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Explore restricted opt-in pull delivery without admitting a receiver API

## Context

[#5125](https://github.com/Cratis/Chronicle/issues/5125) considers bounded long-poll delivery for receivers that cannot keep a stream open. The approved direction is restricted, additive opt-in design only. This proposed contract records the constraints and unresolved questions; it does not admit implementation, establish a public API, or change the security posture. Existing outbound-initiated gRPC observation streams remain the supported path while concrete demand and the predecessor gates in [#5114](https://github.com/Cratis/Chronicle/issues/5114) and [#5104](https://github.com/Cratis/Chronicle/issues/5104) remain unproven.

Today's stream-local `DeliveryId` is an in-memory correlation counter. `PendingDeliveries` matches it with a partition, removes completed entries and retains legacy zero-id matching. It is neither a durable lease nor a credential. `ObserverSubscriberResult` represents handled progress, partial failure, deferral and recovery policy. Pull must feed that kernel machinery rather than create another cursor authority.

The current webhook mediator treats a successful HTTP response as whole-batch success. That is not a receiver acknowledgement protocol. [Decision 0020](0020-kernel-http-delivery-contract.md) specifies the accepted future HTTP envelope and result contract, but is at stage `none`. Its acceptance is not evidence that those semantics are deployed. [Decision 0018](0018-partition-ownership-protocol.md) similarly defines partition ownership and compatibility prerequisites without demonstrating their implementation.

## Decision

Keep existing streams as the supported delivery path and limit pull work to a restricted opt-in design until demand, predecessor work and the invariants below are demonstrated. The design is additive: no default change, new receiver endpoint, public-support promise, or relaxation of authentication and authorization is authorized. Endpoint names, configuration spellings and request/response schemas are intentionally not specified here.

### Authorization and isolation

- Require TLS and authenticate every poll, renewal, acknowledgement, failure and release request. Authorize each operation against the event store, namespace, observer, event sequence and partition, including the principal's tenant grants. Authentication alone does not grant access to a named observer.
- Bind high-entropy lease tokens to the authorized principal and exact delivery scope. A token is not a substitute for authorization. Define delegation, grant revocation and identity changes before exposing any operation. Deny anonymous, wrong-tenant and insufficiently privileged callers without disclosing events or changing progress.
- Receiver-to-kernel authentication is separate from kernel-to-receiver envelope signing. A signature does not authorize reads or cursor mutation. Neither restricted placement nor any optional authentication setting on an existing push path is a waiver for pull.
- Bound wait time, batch events and bytes, outstanding leases, renewals and total lease lifetime. Define fair quotas across principals, observers and partitions. Keep tokens and event payloads out of audit logs while recording authorization and fencing outcomes safely.

### Lease ownership and progress

- Permit one current lease per ordering unit under the kernel's partition claim. Durably bind its id and epoch to the full observer key (event store, namespace, observer, event sequence), partition, authorized receiver principal and tenant grants, exact delivered positions, replay context and expiry. Failover must retain enough state to reject stale epochs after reassignment.
- Accept only a contiguous prefix of the events actually delivered under that lease. A numeric position chosen by the caller, an arbitrary subset, a wrong partition or owner, impossible progress and a late or stale acknowledgement cannot advance the cursor. Preserve the distinction between positions handled and positions scanned; neither cursor belongs to the caller.
- Make acknowledgement commit and response a recoverable durable transition binding the validated lease epoch and prefix to kernel progress and lease completion. Duplicate retries must return stable outcomes without advancing twice, including after a crash before or after progress persistence, lease completion or the response. Existing checkpointing does not prove this guarantee.
- Decide renewal-versus-expiry races and multi-partition partial-commit behavior explicitly. Cancellation, disconnect and an empty long-poll timeout acknowledge nothing. Expiry, release and failure return unfinished work to the kernel's retry or park policy, retaining partial outcomes and deferrals.
- Delivery is at least once. A stable deduplication identity is distinct from a fresh attempt or lease token. Redelivery and remote side effects overlapping an expired attempt remain possible; no exactly-once side-effect guarantee follows from cursor fencing.

### Compatibility and lifecycle

Push, stream and pull must be mutually exclusive for an observer. Cutover must drain or fence old work before transferring ownership, with enforceable version and capability gates during mixed-version operation. A lease cannot bypass the partition claim or create a second progress authority.

Reuse the accepted HTTP envelope/result semantics when their prerequisites hold, not today's internal webhook JSON or legacy zero-id matching. Do not change generic webhook success handling or existing stream identifiers as a side effect. The design must cover replay-boundary acknowledgements, stable replay/deduplication identity, reducer state, restart, rebalance and upgrade/downgrade behavior before a contract is accepted.

## Options considered

1. **Public receiver API now.** Rejected for this scope: demand, support boundaries and durable lease semantics are not established.
2. **Restricted additive opt-in design.** The selected direction, with the same authorization and durability requirements as a public surface. Restricted does not mean unversioned or insecure.
3. **Continue with streams only.** The operational default until the design gates hold. Streams do not provide scheduled-function lease semantics, but avoid inventing a premature contract.

## Default if unanswered

Continue to use existing streams. Do not expose a receiver API or infer permission to implement one from this record. Scheduled receivers that cannot maintain a stream remain unsupported by this proposed transport.

## Timeline and scope

This is the bounded, three-day design described by #5125, not an implementation schedule. Before implementation can be admitted, resolve:

- Concrete receiver demand, predecessor completion, intended operators and exposure boundary.
- Allowed principals, delegation, tenant grants and revocation behavior.
- Wait, batch, lease, renewal and fairness limits.
- Durable failover, late-ack responses and external side-effect fencing limits.
- Replay/deduplication policy, reducer support, mode cutover and version gates.
- Whether and when a public versioning and support commitment is justified.

No code, generated contracts, configuration, migration or security-policy change is part of this record. #5125 remains open for those unresolved gates and decisions.

## Verification

**Done when:** a separately reviewed design resolves the questions above and demonstrates that authorization, ownership, recovery and cursor invariants can hold. This documentation change does not meet that future implementation gate.

**Verify by:** source-backed design review and a specified hostile/crash-case matrix covering anonymous and low-privilege callers; wrong tenant, event store, namespace, observer, sequence, partition and principal; revoked grants; stale epochs; malformed or non-contiguous prefixes; duplicate and late acknowledgements; crashes around each durable acknowledgement transition; renewal/expiry races; cancellation; quota exhaustion; failover; replay; reducer state; and mixed-version cutover. Demonstrate stable duplicate responses and no skipped events or double advancement. Runtime evidence belongs to later implementation work, not this note.

## Consequences

The design preserves one kernel-owned cursor and partition owner and avoids promising an unsupported public contract. It requires durable lease state, explicit authorization and crash recovery before implementation, so pull cannot be treated as a thin wrapper over the current delivery counter. Restricted opt-in still carries compatibility, operational and security obligations.

## Source evidence

Current behavior was checked in `Source/Kernel/Grpc/Observation/PendingDeliveries.cs`, `Source/Kernel/Grpc/Observation/Reactors/Reactors.cs`, `Source/Kernel/Contracts/Observation/EventsToObserve.cs`, `Source/Kernel/Core/Observation/ObserverSubscriberResult.cs`, `Source/Kernel/Core/Observation/Webhooks/WebhookMediator.cs` and `Source/Kernel/Server/UserIdentityMiddleware.cs`. These establish existing correlation, outcome and identity handling, not the proposed lease guarantees.
