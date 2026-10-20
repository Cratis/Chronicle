---
id: 0015-global-projection-scope-is-restricted
title: Restrict global projection scope and read store-wide statistics from namespaces
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/Kernel/Core/Statistics/**
  - Source/Kernel/Core/Projections/Kernel/**
  - Source/Kernel/Core/Projections/ProjectionsManager*.cs
  - Source/Kernel/Core/Observation/**
  - Documentation/concepts/observers.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Restrict global projection scope and read store-wide statistics from namespaces

Proposed on 2026-10-20. This records the maintainer's implementation direction; formal acceptance remains separate.

## Context

The only built-in global projection is event-type statistics. Its key is `(EventType.Id, Namespace)`, so its shared collection is a disjoint union of the namespaced collections, not a cross-namespace computation.

Each namespace has its own observer and replay job, but globally scoped projections share one mutable sink. A namespace-local replay can switch that sink into replay mode, reset the replay collection, or publish a namespace's incomplete view over another namespace's work ([#5172](https://github.com/Cratis/Chronicle/issues/5172)). Materialized reads also resolve the requested namespace rather than the shared sink ([#4392](https://github.com/Cratis/Chronicle/issues/4392)).

## Decision

Remove shared global statistics materialization. Store-wide statistics enumerate the store's namespaces, read their existing statistics concurrently, and concatenate the rows. The request and response contracts do not change. A failed namespace read fails the query rather than reporting partial totals.

Kernel declarations of `Global` or `Both` scope fail during building/lowering. Registration also skips incoming global kernel definitions with a structured warning and uses the filtered set for full-set retirement, including registrations from old silos. Retain the scope enum and persisted-definition support for existing stores. Refuse full and partition replay of global definitions before admitting jobs or mutating sink state, including replay resume and completion callbacks. Definition evolution retains a recommendation instead of automatically replaying a global projection.

This supersedes the unmerged draft of decision 0015 on `decisions/5172-global-replay-ownership`; that draft is not part of this decision.

## Rejected alternatives

- **Coordinated owner over a fixed-name shared sink:** the earlier draft's grants, drain fence, 30-second pause, backup copy, and administrative recovery are disproportionate for a union and duplicate [decision 0014](0014-projection-generations.md)'s binding protocol.
- **Single multi-source observer:** requires cross-namespace observation and recovery semantics for a result already available by reading each namespace.
- **Global event sequence:** introduces cross-namespace ordering and tenant-boundary concerns to avoid query-time reads.
- **Namespace-sliced shared sink:** still needs shared-container lifecycle ownership; separate namespaced sinks already provide the isolation.

## Default if unanswered

Keep the shared statistics sink and refuse its replay. This retains duplicated storage and the wrong-namespace materialized-read problem without enabling a safe rebuild.

## Timeline and scope

Apply the restriction in the next patch; keep it until generation ownership exists. Scope is built-in kernel declarations, store-wide statistics reads, and defensive replay admission for persisted global definitions. A new shared-container ownership protocol, destructive data cleanup, and a rolling-upgrade fence are out of scope.

## Verification

**Done when:** kernel statistics have only a namespaced definition, store-wide reads return the union, retired global definitions disappear on full-set registration, and global replay cannot reach a sink.

**Verify by:** run the statistics union/empty/failure specs, kernel builder Global/Both refusal specs, retirement and old-registration specs, and full/partition replay admission/resume specs, then the Tier 1 build/spec/documentation gates.

## Consequences and upgrade

- Store-wide statistics perform N reads for N namespaces, with `MaximumRows` applied per namespace. They are not an atomic cross-namespace snapshot; pagination within a namespace remains unchanged.
- Full-set kernel registration retires `$system.statistics.event-types.global`: it retires its observers, deletes their jobs, unregisters the engine definition, and removes the projection definition. The namespaced projection and read-model definition remain.
- Leave the orphaned `NotSet` container intact. Retirement has no safe, scope-specific container cleanup hook; removing the shared read-model definition would also affect namespaced statistics. The new statistics reader does not read that container.
- Once the ProjectionsManager grain runs new code, it skips global kernel definitions regardless of registration order. An old silo's full set cannot recreate the writer and still retires a stored global definition even when that incoming set includes it; its other kernel projections continue registering. Stored global definitions remain readable. The remaining re-registration window is only while that grain is hosted on an old silo. New silos still refuse global replay. Avoid replay during the mixed-version interval, finish upgrading all silos, and run full-set kernel registration on the updated grain. This is a registration restriction, not a new ownership protocol.
- Retirement requests job deletion, including running jobs. This does not establish a cross-silo drain guarantee for work already executing on an old silo. The removed sink is no longer authoritative for new store-wide statistics.
- The wrong-namespace materialized-read problem is moot for built-in statistics, not repaired for arbitrary legacy global definitions.

## Future global materialization

Global materialization returns only as a section of [decision 0014](0014-projection-generations.md): generation bindings, fencing by addressing, and a per-contributor Following vector. Never promote a candidate with failed steps. Track that work in [#5159](https://github.com/Cratis/Chronicle/issues/5159), rather than building a second ownership protocol around a fixed-name sink.
