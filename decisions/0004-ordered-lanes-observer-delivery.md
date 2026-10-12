---
id: 0004-ordered-lanes-observer-delivery
title: Deliver observers through ordered lanes over a declared sequencing key
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-11
decider: woksin
applies-to:
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Configuration/**
  - Source/Kernel/Storage*/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Grpc/Observation/**
  - Source/Kernel/Server/**
  - Source/Clients/**
  - Source/Workbench/**
  - Source/Tools/**
  - Documentation/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Deliver observers through ordered lanes over a declared sequencing key

Proposed on 2026-10-11; woksin is the intended decider, pending acceptance.

## Context

Issues #4583, #4600 and #4748 expose the same mismatch: delivery is partitioned, but progress belongs to the observer or to separate catch-up and repair jobs. A live event can be filtered out or dropped while another path advances the shared cursor past it. `Observer.Handling.cs` filters against observer-wide `NextEventSequenceNumber`; completion uses observer-wide `LastHandledEventSequenceNumber`. Neither establishes delivery for a particular key.

The documented promises are at-least-once delivery, ordering per event source, and failure isolation to one partition. Projection catch-up currently stops at a new failure across partitions, unlike live delivery. Repair, catch-up and live delivery need one progress authority rather than more coordination between competing cursors.

This changes durable progress, delivery contracts and upgrade boundaries, and rejects plausible smaller fixes. It therefore warrants a durable decision. [Decision 0002](0002-event-generation-preservation-and-delivery.md) continues to govern generation selection, revisions and redaction; this record does not supersede it.

## Decision

Chronicle uses **Ordered Lanes**: append notifications only wake an ordered durable reader, hashed lanes serialize delivery over a declared sequencing key, and durable obligations preserve every unacknowledged applicable event. Delivery is ordered and at least once per key, with parking as the default failure policy. The engine remains dormant until explicit cluster finalization in a minor release and becomes mandatory in the next major release. The following requirements define the proposed contract.

### Q1 — Park a failed key by default

All observer types, including coupled projections, default to `Park`: the failed event and its successors wait while other keys continue, including keys in the same lane. Retry begins at the first unsuccessful event. Retain 10 attempts, 1-second initial backoff, factor 2 and a 600-second cap, followed by key quarantine without releasing successors. Existing observer quarantine thresholds retain “0 = never”; introduce no new ceiling.

`PartitionFailurePolicy` and the projection builder offer opt-in `Stop` and `SkipAfterRetries`. Stop halts the observer at the event. SkipAfterRetries records a dead letter before continuing; operator `SkipEvent(key, sequence)` also records the skip. Clearing failed partitions records skipped ranges before removing obligations. A released coupled key that crosses other keys' events raises `RebuildRecommended`.

### Q2 — Repair and partition replay pause only their key

Live events remain in the log. Alternate one forward batch of B events with one repair batch of at most 2B events for a due key, bounded by the lane position at repair start. Serve the other side immediately when one has no work; never freeze forward delivery for an entire repair. Other lanes continue independently.

Each repair turn relinquishes its slot, even on failure. Persist oldest-due scheduling with monotonic time and increasing tickets; requeues and operator retries join behind eligible work. A continuously eligible key with D eligible or in-flight turns ahead starts by the (D+1)th repair opportunity, including an already-running turn. This is a service bound, not a wall-clock or overload-completion guarantee. Full replay and definition-change replay pause the observer; there is no blue/green live delivery.

### Q3 — Preserve replay obligations and expose the result

Projections may promote a rebuild with failed keys retained as parked obligations. Reducers retain abandon-on-failure by default; `ReplayPromotion(PromoteWithParked)` opts in. Report how many promoted keys remain stale. Stop-policy observers never partially promote; the old generation remains intact.

Selected-type replay is bounded per key by the lesser of lane progress and its first unresolved position, never resolves parked work, and runs automatically only when independence is proven; otherwise use full replay. Isolated observers rebuild a single key while others flow. Coupled projections perform and report in-place reprocessing, preserving redaction/revision behavior. Add `ReplayPartitionWithResult` with `Rebuilt`, `Reprocessed`, `Queued` or `Refused(reason)`; retain the existing task-returning operation and add replay abandonment.

Full replay builds an inactive generation and promotes idempotently through a persisted promotion state, generation-addressed sink swap and installation of its progress and obligations. Shared cross-namespace sinks never generation-swap: they reprocess in place, are coupled, and are excluded from cross-namespace rebuild certification. Reactor historical passes retain `OnceOnly`/`Replay`; parked suffixes use ordinary delivery mode.

### Q4 — Promise ordered at-least-once delivery, not exactly-once effects

Every applicable event is acknowledged, pending delivery or replay, parked, or explicitly recorded as skipped. First deliveries increase in sequence number per key. Each invocation starts at its first unacknowledged event, except explicit historical reprocessing. Effects can repeat or arrive out of order for these reasons:

1. A handler performs an effect and then fails.
2. A crash after acknowledgment but before checkpoint repeats the pinned batch.
3. A timeout or disconnect leaves an abandoned call running; its effect can arrive after newer effects.
4. Full, key or selected-type replay, redaction/revision rewind, or dead-letter re-drive intentionally reprocesses history in ascending order within that operation.
5. A projection sink flush fails after a partial write.

Timeouts and disconnects park the key with their existing failure kinds and retry from the uncertain batch's first event. Timeouts still do not count toward observer quarantine. Attempts are fenced; late results never advance accounting. On finalized clusters, SDKs cancel the handler token on timeout and serialize calls per observer/key within one process; this is inert without kernel attempt metadata and cannot fence effects across processes.

Keep the six-component `ReactorDelivery.Id` unchanged, with the key's display string as Partition. `DeliveryAttempt` carries number, redelivery flag and reason outside identity/equality. Exactly-once effects require idempotency or a transactional receipt keyed by delivery identity. Projections acknowledge only after durable sink flush.

### Q5 — Roll forward through a bridge and explicit finalization

Support rolling upgrades without a cluster stop: required bridge minor, engine-capable minor with legacy delivery active, then manual online `cluster finalize-upgrade`. `observers.finalizeOnStartup` is an explicit single-silo/development opt-in. `cluster upgrade-status` distinguishes admission, fencing, irreversible reservation and migration; abort is allowed only before the reservation.

Admission, fence, reservation and abort share one serialized cluster record. Admission leases are revocable and expiring, bound to silo identity/generation, version and token. The actual Orleans membership-acceptance boundary must reject expired or revoked permits; a startup check followed by unrestricted joining is insufficient. Active members must be engine-capable; unregistered or incompatible members block cutover.

Reserve the first cutover atomically before any observer format marker. Reservation permanently disables abort, even if no marker exists yet; abort and reservation race through the same compare-and-swap. After reservation, rollback requires backup restoration. Migration locks suppress legacy deliveries, jobs and callbacks; migrate observers individually, import earliest unresolved failures, choose conservative starts from legacy progress and unfinished work, resolve or block uncertain replay promotion, and write each format marker last. Revalidate membership before markers and resume idempotently after crashes.

### Q6 — Hash a declared sequencing key into lanes

The partition is a kernel-evaluated declarative sequencing key, defaulting to event source ID. Its vocabulary is event source, event property, context property, composite or constant; evaluation uses neither client calls nor database lookups. Registration accepts only event-source keys until #4768 supplies non-default reducer keys, analysis, fan-out and indexes. Reducer state must be keyed by the same ordering unit to prevent concurrent updates from different source streams.

A key-definition change requires full replay into a new generation; old rows are never reinterpreted. Unresolvable, empty or oversized keys park visibly under a reserved unresolved key per event type, never a default key. Canonical keys include kind and length-prefixed UTF-8 parts with invariant GUID/integer forms, capped at 8 KiB. Store the full key and its SHA-256 identity, not legacy width-limited partition columns.

Use `xxHash32(canonicalKey) mod 1024` slices grouped into lanes. Isolated observers default to 16 lanes, configurable up to 64; coupled observers use one. Reactors and reducers keyed by their sequencing key are isolated; projections qualify only when event-source-keyed and not Global. Unknown constructs are coupled. One lane alone does not promise global order: `ObserverOrdering.Global` requires one lane **and Stop**. Remap only when every lane is idle at a common position, preserving key identity.

`observers.laneBatchSize` defaults to 128. `maxConcurrentPartitions` retains its meaning: together with `jobs.maxParallelSteps`, it bounds replay/rebuild steps and detached catch-up lanes, not attached live lanes.

### Q7 — Treat pre-upgrade history as unverified

Already-lost events cannot be detected from progress state. Publish a known-issue page and release guidance, without automatic replay. Each migrated lane records its conservative cutover boundary U; completion below U is `Unknown`, and the Workbench displays unverified history. Read-model certification requires a full engine-generation rebuild and promotion, not state diffing. Shared sinks remain excluded. Reactor operators reconcile external effects using key replay and receipts.

### Q8 — Base completion on per-key evidence

Retain `appendResult.WaitForCompletion()` but implement it using `handled(observer, key, sequence)`: `Handled`, `NotYet`, `Unknown` or proven `NotApplicable`, with explicit `Parked` and `Skipped` non-success outcomes. Only Handled and NotApplicable map to success for older clients. Unknown ends the wait without success; checkpoint notifications replace 50-millisecond polling. Passive/inline read models remain the strong read-your-writes option.

Bounded append requests derive targets from the committed log, evaluating every candidate key/event without filtering out unknown applicability. Missing first positions or ranges exceeding 10,000 events use conservative prefix-wide proof, not append-isolated inference. A lane position past the target and no failures are necessary progress checks, never sufficient delivery proof. Prefix and individual-target answers obey the same evidence rules.

### Reader, accounting and fencing requirements

- Notifications carry wakes, not event payloads or progress. Coalescing, spill and lost wakes affect latency only; a 5-second namespace sweep and minute reminder provide recovery.
- One durable reader scans ascending pages below an exclusive committed fence. Every position below it is committed or permanently absent; append draining alone, the tail and maximum sequence number are not proof. Providers must establish this under concurrent writers. `Events`, `Exhausted` and `Error` are distinct; only Exhausted proves absence, and errors advance nothing.
- Lanes account contiguous scanned ranges without bridging gaps. Full bounded buffers detach into self-paging catch-up; excess detached lanes wait fairly without advancing. Cache and filter optimizations preserve revision/generation selection and retained replay obligations.
- Each lane has at most one outstanding invocation, executed outside grain turns. Persist generation, activation epoch, lane attempt and pinned batch bound before dispatch. Accept only the current attempt; durably retire ambiguous attempts before replacing them.
- Root progress, per-key failure/repair obligations and recorded skips change atomically under root revision compare-and-swap. Obligations exist before forward progress crosses their events; release and obligation deletion are atomic. Other-lane writes require retry and attempt revalidation, not stale-result acceptance.
- Keep healthy keys row-free and delivery memory bounded by lanes × batch size plus one retry page. Page activation, scheduling, evidence and administration reads. A pure delivery state machine separates decisions from its Orleans host.

## Options considered

- **Shared observer cursor (today):** smallest change, but competing live, catch-up and repair authorities can skip events and falsely report completion. Rejected as the end state.
- **Design A, per-partition progress with coordinated delivery cycles:** rejected as unsound after adversarial review. Adding progress authorities does not establish one durable, fenced delivery path across live delivery, recovery and handover.
- **One ordered scan per observer without lanes:** simplifies the cursor, but loses partition isolation and parallelism; repair or poison work can stall unrelated keys and undermine existing configuration semantics. Rejected.
- **Ordered Lanes:** chosen because one ordered source plus per-key obligations preserves failure isolation, parallelism and conservative completion without keeping progress rows for every healthy key.

[Axon's sequenced dead-letter queue](https://docs.axoniq.io/axon-framework-reference/5.3/dead-letter-queue/) holds successors of a failed sequence, and its [streaming processors](https://docs.axoniq.io/axon-framework-reference/5.3/events/event-processors/streaming/) hash sequencing identifiers into segments. Chronicle uses lanes within an observer rather than distributed segment claims; Axon's queue does not supply Chronicle's completion proof.

[Kurrent persistent subscriptions](https://docs.kurrent.io/server/v24.10/features/persistent-subscriptions.md) park exhausted retries but do not hold successors. [Akka error handling](https://doc.akka.io/libraries/akka-projection/current/error.html) makes skipping explicit. [Marten's error-policy change](https://github.com/JasperFx/marten/pull/2954) defaults to skipping live apply errors but not rebuild errors; Chronicle instead requires opt-in skipping. [Akka JDBC](https://doc.akka.io/libraries/akka-projection/current/jdbc.html) ties exactly-once processing to transactional effects and offsets. [Marten multi-stream projections](https://martendb.io/events/projections/multi-stream-projections.html) illustrate why reducer ordering must follow the destination key. These precedents inform, but do not prove, this design's fencing or completion guarantees.

## Default if unanswered

Legacy delivery remains active and progress remains vulnerable to the loss and false-completion cases above. Fail-closed storage corrections can ship independently, but cannot establish the new delivery contract. Neither publishing this proposal nor installing dormant engine support authorizes finalization.

## Timeline and scope

The contract spans the opt-in minor releases through mandatory adoption in the next major. It covers reactors, reducers, projections, storage providers, clients, operator tools and delivery documentation. Non-default keys belong to #4768; arbitrary database-backed key grouping, exactly-once external effects, blue/green delivery and cross-namespace shared-sink coordination are out of scope.

| Phase | Shippable scope | Release label |
| --- | --- | --- |
| 0 | Signal-driven loss reproductions for #4583/#4600/#4748 and 33 active partitions; baseline benchmarks and known-issue guidance. | no-release for test/docs-only changes |
| 1 | Legacy storage errors fail closed rather than becoming NotFound (#4770); correct existing documentation. | patch |
| 2 | Dormant bridge: committed pages/fences, atomic stores, key representation, migration guards and versioned coverage/admission envelopes. | minor |
| 3 | Dormant core/reactor lanes, wake-only delivery, fair repair, fencing, SDK attempt handling and sweeper; invert loss reproductions. | minor |
| 4 | Dormant reducers/projections, durable flush acknowledgment, generations, classification and replay results. | minor |
| 5 | Spec-first coverage/handled/completion design and implementation; operator views, CLI and metrics; legacy behavior unchanged. | minor |
| 6 | Spec-first admission/finalization design and implementation; enable only by explicit finalization after correctness and performance gates. | minor, opt-in |
| 7 | Automatically finalize and remove legacy delivery, catch-up and retry-job paths. | major |

Phase 5 deliberately finalizes coverage schemas, retention/compaction and completion algorithms only after specifications prove these fixed invariants: applicability and acknowledged delivery are separate; historical replay is applicable-but-pending until acknowledged; removing a type never cancels obligations; incomplete or compacted evidence yields Unknown before absence-based NotApplicable. Publish immutable evidence through an atomic head change. Preserve open types, pending replay and pinned obligations even beyond 256 records; only compactable closed history has that limit. Read a stable root revision around evidence, retry once on change, then return Unknown.

Phase 6 deliberately finalizes admission leases, actual Orleans join enforcement, migration and abort protocols after specifications at the real membership boundary. The reservation-before-marker rule and safe lease revocation are fixed, not optional implementation choices. If revocation can race membership acceptance, finalization cannot ship. Coverage and admission envelopes in phase 2 reserve extension points, not speculative later-phase implementations.

## Verification

**Done when:** every applicable event is acknowledged, pending, parked or recorded skipped, first deliveries are ordered per key, unrelated keys continue during repair, and completion never reports false success. Finalization safely excludes incompatible members and survives crashes without reopening rollback after reservation. The opt-in release preserves legacy behavior until finalization; the next major removes it.

**Verify by:** run deterministic specifications and integration tests on MongoDB, SQL and InMemory across all observer types, including the three loss reproductions, 33 active partitions, every persistence-boundary crash, duplicate activations, stale results, hung calls/reconnects, partial sink writes, replay promotion and fairness under unlimited poison retries. Assert exactly-once effects only with a receipt fixture.

Phase-5 proofs cover successive type additions, removal/re-addition, more than 256 open types, compaction with retained failures, partial historical replay, crashes around evidence publication, and identical conservative rules for bounded and prefix completion. Phase-6 proofs race admission, lease expiry/revocation, membership acceptance, abort and reservation; crash before the first marker and during migration; and show incompatible membership or uncertain promotion blocks safely.

Before phase 6 ships, measure on an 8-vCPU/32-GB rig with a local three-node MongoDB 7 replica set and 1-KB events, against the phase-0 baseline only where its output is complete. Use B=128, open-loop load, 2-minute warm-up, 10-minute measurement and five repetitions; gate on medians and report spread. Required gates:

- G0: accounting/order correctness and zero false completion, including NotApplicable.
- G1: at 2,000 events/second, 20 observers and 10,000 keys, p50 latency ≤ baseline + 5 ms; p99 ≤ max(1.25 × baseline, baseline + 25 ms) and ≤ 100 ms, including durable/query-visible completion.
- G2–G3: million-event catch-up ≥ 80% of baseline for reactors/reducers and ≥ 90% for projections; alternating-key throughput ≥ 60%. If lanes increase to 32 to meet catch-up, repeat every performance gate.
- G4: per 1,000 deliveries, warm documents examined ≤ 1,200 live/1,100 catch-up, queries ≤ 20, root writes ≤ one per batch, bytes ≤ 1.25 × baseline; cold/four-detached-lane documents ≤ 2,000 and bytes ≤ 2 × baseline.
- G5: 100,000-event repair at 50% lane capacity keeps other-lane p99 ≤ 1.1 × baseline and same-lane p99 within one repair batch of baseline; prove the D+1 fairness bound even across restart and operator retry.
- G6–G9: lost-wake delivery within 10 seconds; RSS growth ≤ 50 MB from 10,000 to 10 million keys; first cold delivery within 2 seconds at one million keys; million-event projection replay ≤ 110% of baseline duration.

## Consequences

Progress has one meaning across live delivery, catch-up and repair. Operators can see parked keys, recorded skips, replay outcomes and unverified history instead of inferring delivery from a shared cursor. Existing clients retain additive compatibility; release notes must name every opt-in behavior change.

The cost is rereading the log per observer, transactional progress/obligation updates, retained applicability evidence and a more demanding upgrade protocol. Healthy keys need no durable rows, but active and pinned evidence cannot be forced into a fixed-size history cap. Hot keys remain sequential, and coupled repair can require rebuilding. Fencing protects accounting, not external effects of abandoned calls. Historical loss remains an operational reconciliation problem rather than something a new cursor can repair retroactively.
