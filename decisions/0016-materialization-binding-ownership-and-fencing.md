---
id: 0016-materialization-binding-ownership-and-fencing
title: Publish materializations through one binding and require storage-enforced ownership and fencing
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-20
decider: woksin
applies-to:
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/ReadModels/**
  - Source/Kernel/Core/Sinks/**
  - Source/Kernel/Storage*/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Publish materializations through one binding and require storage-enforced ownership and fencing

Accepted by woksin on 2026-10-20; not implemented or provider-verified. This is the publication protocol for [decision 0014](0014-projection-generations.md), subject to [decision 0017](0017-generation-enablement-compatibility-and-recovery.md)'s initialization/floor. [Decision 0015](0015-global-projection-scope-is-restricted.md)'s global restriction remains in force.

Amendment 2026-10-22 (measured MongoDB fence and DDL rules):

This banner adds measured evidence and records how the MongoDB and DDL rows below are met. It does not change the decision text. Tracked in Cratis/Chronicle#5225.

- **Topology.** MongoDB requires a replica set; a single-node replica set is accepted. A standalone mongod fails closed when generations are enabled, and the refusal tells the operator to start mongod with `--replSet`. It never writes unfenced.
- **Measured fence cost** (MongoDB 8.2, same-machine containers, a heavily loaded host, so ratios between back-to-back runs are the usable signal). Without change streams, a fenced batch used 0.3-0.8x the unfenced server CPU and about 0.55x the oplog bytes, with 1.2-1.7x the throughput at 500-1000 documents per batch, because a transaction writes one oplog entry where an unfenced bulk writes one per document. Eight parallel keys on one collection produced no retries and no aborted transactions, so per-key gates with one lane per sink need no extra writer lanes. With N open change streams, fan-out dominates both modes (fenced/unfenced CPU 0.97-1.27x up to N=500), but fenced latency absorbs it: at 500 streams and 1000-document batches the median batch took 8 s fenced versus 0.6 s unfenced, while batches of 100 stayed near 0.5 s.
- **Fenced unit.** One fenced unit is one majority transaction containing the conditional gate increment, the writes and the receipt. A larger flush is split into several such units at one configured batch cap (default 500 documents), the same kernel setting as the replay page cap introduced for Cratis/Chronicle#5262.
- **DDL is not fenced.** MongoDB cannot run drop or rename in a transaction, and an epoch check before and after an operation only detects a stale operation. Each generation therefore uses its own container (`{container}-g{n}`). Publication changes which generation is served through a fenced binding write and never drops or renames a served container. Destructive DDL (drop, rename, clear) is refused on the container of an Active or Retired generation. Drop and rename run only for Retired or Abandoned generations after the exclusion work in Cratis/Chronicle#5226. Index creation on a Building generation's own container is allowed. Until per-generation containers exist, the replay promotion path (renames over the served container) stays unfenced and generations stay behind the `ProjectionGenerations` flag (default off).
- **Vanished writer.** An open transaction held by a vanished writer blocks takeover until the server reaps it (`transactionLifetimeLimitSeconds`, default 60 s; measured at 5 s with that limit lowered to 5 s). The default of roughly 60-70 s is accepted. Tuning (`maxCommitTimeMS`, driver timeouts) is a follow-up.
- **Retired retention until #5226.** Only the most recent Retired generation keeps following. Older Retired generations stop following and stay on disk until exclusion allows cleanup.

## Context

Immutable destination names prevent accidental redirection but do not revoke a delayed writer, cleanup or schema operation. An Orleans owner can orchestrate work; single activation, job completion and a metadata CAS do not exclude stale provider mutations.

Existing execution identity is narrower than a generation: [sink cache keys use type, configuration, container and sequence](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage/Sinks/Sinks.cs#L27-L38), [replay contexts are keyed by read-model identifier](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage/ReadModels/ReplayContexts.cs#L16-L30), and [reducer subscriptions/dispatch use reducer, connection, store and namespace](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Observation/Reducers/Clients/ReducerMediator.cs#L24-L56). These are extension points, not existing generation fences.

Future saves expose another boundary: [the sink write precedes resolving the pending future](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/Engine/Pipelines/Steps/SaveChanges.cs#L130-L156), while [future storage resolves by projection identity](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/ProjectionFuturesStorageProvider.cs#L20-L34). A crash between state and receipt must be idempotently recoverable; a renamed observer alone cannot isolate it.

## Decision

Publish each materialization by one conditional, durable binding commit with its receipt, after proven coverage and storage-enforced revocation/drain of every affected writer. Require transaction-capable MongoDB topology initially and provider-specific transactional enforcement on SQL; require storage-enforced takeover long-term, but refuse promotion/enablement wherever delayed data, DDL or cleanup exclusion is unproven. Orchestration records progress; only storage authority settles publication.

### Authority and linearization

The materialization key includes store incarnation, logical projection/reducer identity, scope and sink. Its authoritative binding records Active generation, immutable execution revision/schema, physical destination, serving epoch, owner/fence identity and publication receipt. Check expected old generation/epoch and attempt ownership in the same storage commit that changes the binding and records the unique operation outcome. MongoDB uses an authoritative binding document and atomic receipt publication; SQL uses a transactional binding row and receipt. Neither uses promotion DDL, an alias or a second catalog pointer.

That commit is the linearization point. Readers acquire a bounded generation retention lease through authority resolution coordinated with deletion; they use one generation/schema for the operation. A read starting after completed publication must observe its binding. An overlapping operation may finish on the prior complete generation. Caches/notifications are hints, never an indefinitely stale serving authority. Catalog Active/Retired and served-definition metadata reconcile from the binding/receipt.

Serving epoch is distinct from generation writer/owner epochs. Legitimate Following, Active and Retired writers continue only under current admission for their own immutable generation; serving a new generation must not redirect an old writer. Binding CAS alone fences neither DML nor DDL.

### Writer revocation and provider capability

Close admission to affected execution owners and revoke their storage grants. A grant binds incarnation, generation, execution specification, owner/attempt, epoch and permitted physical operations. Drain or storage-revoke all work before transfer: projection/reducer batches, bulk buffers, child/join saves, futures, removals, initialization/index creation and occurrence cleanup. Reconcile committed obligations and flushes. A callback checks admission; the provider must additionally reject stale work at mutation commit, including commands delayed before or after admission.

| Provider | Required enforcement and initial support boundary |
| --- | --- |
| MongoDB | Transaction-capable topology is required initially. Each accepted data batch must conditionally modify the generation's fence/gate record and mutate its addressed data/receipt in the same transaction. A read-only epoch check is insufficient: the conditional gate write must conflict with revocation so an old transaction cannot commit after ownership transfers. Binding/receipt publication must be atomic with its ownership conditions. Transaction conflicts, durability and retry/unknown-outcome behavior require topology-specific proof. |
| SQL | Hold the provider's conflicting fence-row lock/conditional ownership update through the same transaction as the addressed data batch and mutation receipt. Revocation/transfer must serialize against that lock so stale DML cannot commit afterward. Validate each claimed provider's isolation, retry and connection behavior; an application-side precheck is insufficient. |
| DDL and cleanup on either family | Prove serialization/exclusion for create/index/drop/cleanup separately. Where DDL cannot participate in the data transaction, use a proven exclusive administrative path with revoked access/sessions or a capability-refuse result. A generation suffix, expiring lease, local lock or database schema helper alone does not prove exclusion. Do not transfer ownership while a delayed destructive operation can still execute. |
| Other/custom/in-memory sinks | Match the same observable semantics and failure behavior or reject generation capability before allocation/mutation. A method returning success is not capability evidence. |

These are required protocol shapes, not claims that provider implementations exist. Initial standalone MongoDB remains on disabled legacy mode. A mandatory transaction per event is not selected: bounded batches may amortize the gate, with correct mutation receipts. No general outbox or universal gateway framework is required.

An unreachable writer cannot be presumed drained. Once storage revocation is proven, takeover may proceed without its acknowledgment, reconstructing committed outcomes. Until then, block/abort before publication and expose the unavailable owner. Clock/lease expiry or cluster membership alone never permits takeover. If authority is unknown, keep fences closed; do not guess that the old owner died.

### Coverage and readiness

Coverage is a durable proof, not the largest scanned position, job checkpoint, lag estimate or absence of observed failures. Persist per-input committed/scanned frontiers, required obligations and source-history revision/invalidation identity. Account for filtered/no-op events, failed partitions, child/join futures, pending saves, buffered mutations, partial adoption and unknown outcomes below a cursor. Data/receipt atomicity or an equivalent proven idempotency protocol must close every crash gap. Reducer recut-batch correctness is separate from ownership fencing.

Choose a stable cut, bring candidate and displaced followers through their own required cut, close admission, reconcile/drain and revalidate membership/history/interpretation before committing. Resume each above its own proven frontier; never copy another generation's job checkpoint. Readiness requires correct schema/required indexes on that physical container, current compatible reducer execution capacity and policy-approved failures. Force can waive only an explicit failure policy, not missing coverage, ownership or history validity.

No shared-global candidate is admitted under this record alone. Future support requires explicit contributor membership/namespace incarnations, committed coverage per input, and deterministic conflict semantics: disjoint ownership, proven commutative operations or one durable aggregate order. Equal contributor vectors do not establish equal noncommutative output. Require provider exclusion as well as vectors under decision 0015; do not build a global stream speculatively.

### Journal, recovery and cancellation

Durably journal operation identity, expected binding, source/target execution bindings, affected writers, cut/obligations, grant epochs and receipts. Serialize/reconcile publication commands before transferring owner epochs. Use phases `Fencing → Draining → Swapping → Swapped → Releasing → Completed`, with pre-commit `Aborting`; Swapping denotes binding publication, never a physical rename.

| Observed durable outcome | Recovery obligation |
| --- | --- |
| Before publication, no committed receipt | Abort/retry only after excluding delayed publication and mutations from the old attempt. Reconcile admitted work before reopening grants. |
| Binding/receipt committed, response lost | Read authority/receipt, finish forward, reconcile lifecycle/served revision and release under current grants. Never retry as another displacement or guess rollback. |
| Authority unavailable or contradictory | Keep fences closed, report recoverable uncertainty; no transfer or deletion by timeout inference. |
| Crash after publication before notification/release | Complete idempotently from the receipt. Notifications carry epoch and trigger authority rechecks; missed notifications cannot preserve stale Active resolution. |
| Cancel/supersede versus publish/rollback/expiry | Resolve through the same serialized ownership protocol. Drain before Abandoned/Archived and honor retention leases before deletion; never delete Active. |

Retention deletion must atomically establish non-Active status, expired/unheld pins and valid ownership before physical removal, with provider exclusion for delayed cleanup. Keep audit/tombstones after removal. Pins do not override compliance revocation. A cancellation or stale cleanup from an earlier attempt cannot remove a published generation or another incarnation's data.

## Options considered

- **Storage-enforced ownership plus one binding (chosen):** supports eventual unreachable-writer takeover with explicit provider cost and proof.
- **Cooperative drain only:** permitted as fail-closed interim behavior, never as proof that an unreachable writer is harmless.
- **CAS, single activation, timeouts or generation names alone:** do not order delayed data/DDL commits against revocation.
- **Physical rename/view publication or multiple serving pointers:** moves destinations or creates competing commit authorities; rejected.
- **Per-event transactions/universal gateway:** broader than necessary; bounded transactional batches and narrow proven administrative exclusion are sufficient goals.

## Default if unanswered

The protocol direction is accepted. Until a provider's exclusion and coverage proof passes, leave enablement/promotion closed and expose the blocker; do not substitute a weaker fence to preserve availability.

## Timeline and scope

Applies to each advertised generation capability, rollback and repair. Implement isolated, measured provider prototypes before production enablement. Cross-database/multi-sink atomic publication, arbitrary global projections and general parallel scheduling are out of scope. Execution and release tracking remain in [epic #5155](https://github.com/Cratis/Chronicle/issues/5155).

## Verification

**Done when:** every advertised provider/topology proves linearization, stale-operation exclusion, coverage and idempotent recovery with nonempty data and independent expected output. Acceptance is not provider certification.

**Verify by:** executable transaction/lock prototypes and failure injection at every journal, gate, batch, receipt, binding, notification and release boundary. Delay old writes/index/drop/cleanup before and after checks; lose commit responses; kill owners; partition two silos; revive stale attempts; race pins/expiry/cancellation/rollback/erasure. Show no post-revocation old mutation, mixed-generation read, partial publication, premature release or incorrect retry. Exercise futures' write/resolve gap and recut reducer batches. Verify unsupported capability refusal before mutation, all index/read roles and released observable reconnect behavior.

Measure fenced throughput/p95-p99 latency, abort/conflict rates, handover duration/unavailability, lease cost, storage/follower amplification and reducer-host capacity against declared budgets. Compare full output to an independent oracle, not only cursor equality. Failure or unaffordable budgets block the claimed slice; they do not waive fencing.

## Consequences

Storage authority makes publication outcomes recoverable and allows safe takeover once proved. Gate contention, transaction support, DDL exclusion and durable receipts add cost. Providers can ship independently only as complete tested slices. Until those proofs exist, failing closed is intentional behavior, not an implementation shortcut or a new availability promise.
