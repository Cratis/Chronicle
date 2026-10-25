---
id: 0022-per-application-event-store-entitlements
title: Scope what an authenticated application may do to explicit, revocable per-event-store entitlements, introduced in phases from documentation and audit to opt-in enforcement
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-25
decider: woksin
applies-to:
  - Source/Kernel/Core/Security/**
  - Source/Kernel/Core/EventStores/**
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/Observation/EventStoreSubscriptions/**
  - Source/Kernel/Grpc/**
  - Documentation/security/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Scope what an authenticated application may do to explicit, revocable per-event-store entitlements, introduced in phases from documentation and audit to opt-in enforcement

Proposed. Not accepted, and nothing in this record changes kernel behavior yet. The direction (per-application entitlement, phased) was chosen by the coordinator as a reversible decision for [#5410](https://github.com/Cratis/Chronicle/issues/5410). The grant contract, the migration deadline, the audit and enforcement releases and the disclosure level remain open for the owner (see [Open questions](#open-questions)).

## Context

### What exists today

Chronicle authenticates callers, but an authenticated application is trusted with every event store on the kernel. Nothing ties an application credential to the stores it serves. Administrator policies cover user and application management, jobs, observer recovery and development tools; ordinary store work is not scoped. In particular any authenticated application can:

- append to any event store;
- read events from any store, including personal data decrypted by compliance release (`Source/Kernel/Core/Sequences/EventSequenceQuerying.cs`, `ReleaseCompliance`);
- register a deployment for any existing store (`Source/Kernel/Core/EventStores/RegisterDeployment.cs`);
- create and use namespaces;
- subscribe one event store to another. The subscription forwards appends into the target store and copies encryption keys with them (`Source/Kernel/Grpc/Observation/EventStoreSubscriptions/EventStoreSubscriptions.cs`, `Source/Kernel/Core/Observation/EventStoreSubscriptions/EventStoreSubscriptionObserverSubscriber.cs`).

This is acceptable when one kernel serves one application or one team that trusts all its applications. It is not acceptable for a shared kernel with mutually untrusted applications, for Workbench users who should see only some stores, or for multi-tenant hosting where a tenant must not reach another tenant's store.

### Why it needs a record

The change alters what a credential means. It touches every store-scoped surface (commands, queries, gRPC services and streams), it needs a migration for existing deployments, and an enforcement default is breaking. Each of those has to be decided once and applied consistently.

## Decision

Chronicle moves from shared trust to **per-application entitlements**: an application may act on an event store only through an explicit grant for that store. It is introduced in phases:

1. **Document** the current trust model, the affected configurations and the mitigations. No behavior change.
2. **Inventory** every store-scoped command, query, gRPC service and stream in a test-only architecture spec, so a new surface without an entitlement classification fails the build. No behavior change.
3. **Audit**: evaluate grants and log would-deny decisions without refusing.
4. **Opt-in enforcement**: refuse calls without a grant when an operator enables it.
5. **Default-deny** in a major release.

Phases 3 to 5 need the owner's answers below before they start.

### Fixed requirements

These hold for any grant contract the owner chooses:

- **Missing caller context is never trusted internal work.** A call that arrives without an authenticated caller is treated as unauthenticated, not as the kernel acting on its own behalf. Internal work carries its own explicit identity.
- **Cross-store subscriptions need explicit delegation.** Subscribing store B to store A requires read on A and write on B. The delegation is revalidated after a grant is revoked; an existing subscription stops forwarding events and encryption keys once either side of the grant is gone.
- **Reads, streams and deployment registration are guarded, not only appends.** Event reads, compliance release of decrypted personal data, observer streams, subscriptions and `RegisterDeployment` are entitlement-checked like appends.
- **Migration covers all applications.** When enforcement arrives, every existing application receives explicit grants. A legacy wildcard grant is explicit and time-limited. It is never a permanent `*` that silently covers stores created later.
- **Revocation is specified and tested.** The record that accepts the grant contract states how revocation affects issued tokens, open streams and running subscriptions, and specs prove it.
- **Scoped tokens narrow grants.** Scoped tokens ([#5131](https://github.com/Cratis/Chronicle/issues/5131)) are a subset of an application's grants, never a way to exceed them. Roles ([#1965](https://github.com/Cratis/Chronicle/issues/1965)) supply grants to users.

## Options considered

### A. Shared trust permanently

Keep every authenticated application trusted with every store, and document it. Rejected for shared kernels: it leaves mutually untrusted applications and tenants able to read each other's decrypted personal data and write to each other's stores. It remains a valid deployment choice where one trust boundary owns the kernel, and the documentation says so.

### B. Per-application entitlement, phased (chosen direction)

Grants per application and store, introduced through documentation, inventory, audit, opt-in and default-deny. It keeps current deployments working until the operator opts in, and gives operators audit data before enforcement.

### C. A kernel per trust boundary only

Treat a separate kernel as the only isolation unit. It is the recommended mitigation today, but as the only answer it rules out shared hosting and multi-tenant kernels.

## Open questions

These belong to the owner and block phases 3 to 5:

- The grant schema and contract: granularity (store, namespace, event sequence, operation), how Workbench users bind to grants, and whether administrators bypass grants.
- The wildcard migration and its deadline.
- The releases that ship audit (phase 3) and opt-in enforcement (phase 4), and the major that makes default-deny the default.
- The disclosure level: a security note in the release notes, or a security advisory after triage.

## Verification

Before acceptance, review this record independently. Before each later phase:

- The inventory spec lists every store-scoped command, query, gRPC service and stream and fails on an unclassified one.
- Audit and enforcement specs cover appends, reads, compliance release, streams, subscriptions and deployment registration, including a caller without context.
- Revocation specs cover tokens, open streams and running subscriptions, including encryption keys copied by a subscription.
- Migration specs prove every existing application keeps working through explicit grants, and that a wildcard grant expires and does not cover stores created after it.

## Consequences

- Until enforcement ships, the only isolation between untrusted applications is deployment: a separate kernel per trust boundary, network or mTLS isolation, and enforcing the administrator policy for operations. The [security documentation](https://github.com/Cratis/Chronicle/blob/main/Documentation/security/index.mdx) states this.
- Every new store-scoped surface needs an entitlement classification once the inventory spec exists.
- Default-deny is a breaking change and ships only in a major release.
