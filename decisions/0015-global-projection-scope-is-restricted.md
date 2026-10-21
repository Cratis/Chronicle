---
id: 0015-global-projection-scope-is-restricted
title: Restrict global projection scope and read store-wide statistics from namespaces
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-20
decider: woksin
applies-to:
  - Source/Kernel/Core/Statistics/**
  - Source/Kernel/Core/Projections/Kernel/**
  - Source/Kernel/Core/Projections/ProjectionsManager*.cs
  - Source/Kernel/Core/Observation/**
  - Documentation/concepts/observers.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Restrict global projection scope and read store-wide statistics from namespaces

Accepted by woksin on 2026-10-20. This bounded restriction is separate from [decision 0014](0014-projection-generations.md)'s generation product, [decision 0016](0016-materialization-binding-ownership-and-fencing.md)'s provider protocol and [decision 0017](0017-generation-enablement-compatibility-and-recovery.md)'s rollout floor.

## Context

The built-in event-type statistics key is `(EventType.Id, Namespace)`. The former shared collection is a disjoint union of namespaced rows, not a cross-namespace computation. Each namespace has its own observer/replay job, but global definitions share a mutable sink. Namespace-local replay cannot safely own its reset or publication. Materialized reads resolving the requested namespace also do not establish arbitrary shared-sink read correctness.

### Implementation status

Source baseline `fb5968a46ab962af4c50bfd0c1387b933d04aec1` already contains these paths; inspection is not acceptance-test evidence or completion of every requirement below:

| Current source | What remains unproven/incomplete |
| --- | --- |
| [Statistics declaration is namespaced with a composite key](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Statistics/EventTypeStatisticsProjection.cs#L23-L31); [store-wide reads enumerate namespaces, await all and concatenate](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Statistics/StatisticsReader.cs#L53-L60). | Task.WhenAll is not bounded fan-out and exposes no request cancellation here. |
| [Each namespace request asks for page zero and MaximumRows](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Statistics/StatisticsReader.cs#L20-L49). | The current reader does not check completeness/overflow; a truncated page cannot be advertised as an exact total. |
| [Global/Both kernel lowering is refused](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/Kernel/KernelProjectionBuilder.cs#L114-L124); [registration filters global kernel definitions](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/ProjectionsManager.cs#L240-L251). | Filtering on a new grain is not exclusion of old-silo work already executing. |
| [Global full replay has an explicit scope refusal](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Observation/GlobalProjectionReplay.cs#L18-L39). | All admission, partition, resume and completion paths still require regression proof; this is not coordinated global ownership. |

Stage remains none: parts exist, but the complete accepted contract, including bounded queries and overflow handling, is not claimed implemented or verified.

## Decision

Remove shared global statistics materialization and obtain store-wide statistics by bounded, cancellable reads of existing namespace statistics followed by row concatenation, without changing request/response shapes. Fail the query for a failed, canceled or incomplete contributor rather than return apparently exact partial totals. Reject new global kernel materializations and refuse unsafe replay of persisted global definitions until coordinated ownership, deterministic input semantics and provider exclusion are proven.

### Query bounds and consistency

Enumerate the store's namespaces once per request, limit concurrent namespace reads with an explicit finite bound, and propagate cancellation to queued/in-flight reads. Do not continue unbounded fan-out after failure or cancellation. MaximumRows applies per namespace; if a contributor exceeds it or completeness cannot be established, fail explicitly rather than silently sum a truncated first page. Complete pagination may replace this limit only when it proves complete bounded/cancellable reads; never label a page as an exact total.

The result is a union of the successfully completed reads for the enumerated membership, not an atomic cross-namespace snapshot. Namespace creation after enumeration may be absent until the next request; deletion/unavailability of an enumerated contributor fails the request rather than being silently omitted. Contributors can have different freshness and change while queried. No store-wide consistent cut, transaction or frozen membership is promised. Preserve composite row identity and existing aggregation meaning.

### Registration and replay restriction

Kernel declarations of Global or Both fail during building/lowering. Registration skips incoming global kernel definitions with a structured warning and uses the filtered set for full-set retirement, including registrations from old silos. Retain the scope enum and persisted-definition support for existing stores. Refuse full and partition replay of global definitions before admitting jobs or mutating sink state, including resume and completion callbacks. Definition evolution retains a recommendation instead of automatically replaying a global projection.

This replaces the earlier unmerged draft's proposed fixed-name shared-sink owner; it does not establish another ownership protocol.

## Options considered

- **Namespaced statistics with query-time union (chosen):** already matches the disjoint data shape and avoids shared-container lifecycle coordination.
- **Coordinated owner over a fixed-name shared sink:** disproportionate for this union and competes with decision 0016's binding protocol.
- **Single multi-source observer or global event sequence:** introduces cross-namespace ordering/recovery and tenant-boundary concerns without a demonstrated need.
- **Namespace-sliced shared sink:** still needs shared-container lifecycle ownership; namespaced sinks already isolate it.

## Default if unanswered

The restriction is accepted. Where its implementation is incomplete, continue refusing unsafe global replay rather than recreating a shared writer. Do not report a truncated union as complete or infer future global support from this decision.

## Timeline and scope

This is a bounded patch-level restriction for built-in declarations, statistics reads and defensive replay admission. Keep it until coordinated ownership, deterministic input semantics and provider exclusion are proven. Destructive orphan cleanup, a new shared owner and a rolling-upgrade floor are out of scope. Generation implementation/release tracking belongs to [epic #5155](https://github.com/Cratis/Chronicle/issues/5155), not to this patch's admission rules.

## Verification

**Done when:** only namespaced kernel statistics are registered, store-wide reads return a complete bounded/cancellable union or explicit failure, filtered full-set registration retires old globals, and no global replay path reaches a sink.

**Verify by:** nonempty union/empty/failure tests; concurrency/cancellation/overflow tests; namespace creation/deletion/freshness races; Global/Both lowering refusal; full-set retirement and old-registration tests; full/partition replay admission, resume and completion guards. Run the affected specs and applicable repository gates. Source presence alone is not verification of this full contract.

## Consequences and upgrade

- Store-wide statistics perform N namespace reads for N contributors, under a concurrency bound. MaximumRows is per namespace, and overflow is explicit failure unless complete pagination is implemented. Results are not atomic membership/freshness snapshots.
- Full-set kernel registration retires `$system.statistics.event-types.global`: it retires observers, requests deletion of their jobs, unregisters the engine definition and removes the projection definition. The namespaced projection and shared read-model definition remain.
- Leave the orphaned NotSet container intact. Retirement has no safe scope-specific cleanup hook; deleting the shared read-model definition would affect namespaced statistics. The new statistics reader does not use that container.
- Once ProjectionsManager runs new code, it skips global kernel definitions regardless of registration order; an old full set cannot recreate that writer on the updated grain. The re-registration window remains while the grain is on an old silo. Avoid replay during mixed versions, finish upgrading all silos and run full-set kernel registration on the updated grain. This restriction is not a new rolling-upgrade fence.
- Job deletion, including running jobs, does not prove cross-silo drain of work already executing on an old silo. The removed sink is no longer authoritative for new store-wide statistics. Stored global definitions remain readable; arbitrary legacy shared-sink read correctness is not repaired here.

### Future global materialization

Future support uses decisions 0014/0016/0017, never a second fixed-name owner. Require explicit contributor membership and namespace incarnations, committed coverage for each input, deterministic conflict semantics and provider exclusion. Disjoint ownership, proven commutative operations or a durably ordered aggregate input can establish determinism; a contributor vector alone cannot. Failed steps and unknown obligations block publication. Do not build a global stream speculatively or lift the restriction merely because generation bindings exist.
