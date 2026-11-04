---
id: 0024-applied-event-markers-for-source-keyed-joins
title: Make redelivery a no-op for event-source-keyed MongoDB read models with root joins by recording applied-event markers on the document
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-03
decider: L0 (woksin's delegation)
applies-to:
  - Source/Kernel/Storage/Sinks/**
  - Source/Kernel/Core/Projections/Engine/**
  - Source/Kernel/Core/Observation/Jobs/ReplayObserverPartition.cs
  - Source/Kernel/Core/Observation/PartitionReplayMarkers.cs
  - Source/Kernel/Storage.MongoDB/Sinks/**
  - Source/Kernel/Storage.MongoDB/Generations/Fencing/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Make redelivery a no-op for event-source-keyed MongoDB read models with root joins by recording applied-event markers on the document

Accepted on 2026-11-03 by L0 under woksin's delegation. It settles [#5524](https://github.com/Cratis/Chronicle/issues/5524) for the MongoDB sink. SQL is a separate slice, and the shapes outside the rule below go to a follow-up issue.

## Context

Observers are at-least-once. A projection with a root join writes the joined event's values onto every document that references it. When a delivery is repeated, or an older event of the document's own source arrives after a newer join, the repeated write overwrites a newer value. A document-wide watermark does not fix this: it drops the first delivery of an event that is late but was never applied. Observer-level deduplication was rejected earlier because the observer does not know what the sink already applied. Two reviews of the plan found the first designs unsound (a trusted duplicate-`_id` error, packed ordinals, a conditional upsert inside a fenced transaction without a probe), and the final scope is the narrowest one that remains correct.

## Decision

**Which projections are marked.** A root projection is marked when it has at least one root join, every root join keys on the event source id, it has no children, no nested and no from-every, every event type that is not join-only resolves to the event source id, and it is not globally scoped. A global projection shares one destination across namespaces, so equal source ids from two namespaces would alternate one slot and make an applied event eligible again. Everything else keeps today's write modes.

**The rule.** For a marked projection, behind the existing sink-write guard switch and the new `ReadModels.RecordAppliedEventMarkers` switch, each document records which source event it last applied per stream: an `f` slot for the document's own source and a `j.<definitionId>` slot for each root join definition. A slot is `{ s, n }`: the hash of the stream identity scoped by namespace, and the event sequence number as a long. A write applies only when its event is newer than the slot of its stream, and the slot moves in the same write, so an event that was already applied is a no-op. An event delivered for the first time is applied even when it is older than other writes to the document, because only its own stream's slot decides.

**How it is written.** The whole changeset of a marked write is one aggregation pipeline update with a single `$set` stage. Every predicate is evaluated against the document as it was before the update, so the data and its slot either both move or neither does, and no later field sees a slot changed by an earlier one. Each field is `$cond`-guarded by the predicate of the group that writes it; overlapping writes to one field are folded in change order. Values are wrapped in `$literal`, so text such as `$name` is stored as text. The document filter is the `_id` alone, so no conditional upsert and no duplicate-key recheck exist any more. A resolved join applies when the document references the join key it was resolved for, either already or because the write of its own event sets that reference, and its slot does not record a later event. The first condition keeps a stale snapshot from overwriting data after the reference moved on. The second, the "fresh reference" disjunct, lets a reference that returns to an earlier target be filled again, without clearing slots on a reference change. Only plain property sets and unsets are supported; any other update operation throws `AppliedEventMarkerUpdateNotSupported`. Joins on a plain `Joined` change stay classic multi-document updates guarded by their filter, which is already atomic.

**Sequence identity.** The slot keeps its shape, but the event sequence id is part of the `s` stream hash (`StreamHash(namespace, eventSequence, value)`). Two writers of one document that consume different sequences can see equal event source ids and sequence numbers; without the id they would share a slot and suppress each other. The write path passes the sequence the projection reads from (`ISinkAppliedEventMarkers.ApplyChangesOncePerSourceEvent`, `ClearAppliedEventMarkers`), so the `s` of a stored slot and the `s` the clear computes agree. Markers written before this change carry a hash without the id and are never equal to the new one, so they stop protecting the document: a redelivery of an older event is applied again until the stream writes a new marker. No released version wrote markers, so this affects only stores that ran a pre-release build with the setting turned on. One slot per join still lets alternating sequences on the same document overwrite each other's marker; a slot per sequence is a follow-up.

**Change order with a join.** A marked write keeps the ordering of an unmarked one. When a keyed From and a root join change the same property, the changeset is written as segments (a join and the direct changes after it), and every segment records a slot of its own (`f.<join definition>`, `f.0` for the leading direct changes). The join of a segment still runs before the guarded own update of that segment, which is correct inside it. A redelivery skips exactly the segments that were written. The alternative, folding the joins into one guarded pipeline, was not chosen: it would need a pipeline form of the multi-document join, while segmenting reuses the ordering the unmarked path already has and proves.

**Reference after the own write.** The reference a resolved join compares with is the value the document has once the same write has applied: a set of the property itself, or a set or removal of an object the property is inside (`customer` replaced decides `customer.id`), read from the written value.

**The switch.** `ReadModels.RecordAppliedEventMarkers` is off by default and applies to the MongoDB sink only. The pipeline update requires MongoDB 5.0 or later. Turning it off takes effect on the next write. Markers left in documents from an earlier period are stale, so before turning it on again after a period off, stop and drain the writers, then either `$unset: { __applied: "" }` on the affected collections or replay the projection. Partition replay clears markers only while the switch is on.

**Fencing.** A fenced sink splits an oversized changeset across transactions only for updates that are safe to apply again. A pipeline update is accepted only when it was produced by the marker renderer and consists solely of `$set` and `$unset` stages: every field is guarded by its slot predicate, so applying it again changes nothing. Any other pipeline is refused as before.

**Invariant.** A document write for a marked projection is applied if and only if its source event is newer than the slot of its stream on that document, and, for a resolved join, the document references its join key. Writes that cannot carry a condition, such as null-parent repairs, record nothing.

**Partition replay.** A partition replay keeps today's semantics: it rewrites the partition from scratch. So it clears the partition's markers when it starts and again when it resumes after a crash. Clearing is idempotent and global to the sink, so a single call is enough. The partition's own document and the joined documents whose slot belongs to that partition are cleared. Resume does not begin the replay again, because that would notify reactors and reducers twice.

## Rejected alternatives

- Routing partition replay to a full replay: breaks selected-partition semantics and misses persisted jobs.
- A document-wide watermark: drops late first deliveries.
- Observer-level deduplication: the observer cannot know what the sink applied.
- Protecting children, nested, from-every and collapsed writes now: follow-up issue.
- Per-property provenance and raw source ids as field names: size and layout cost without a correctness gain.
- InMemory and SQL: the in-memory sink ignores every write mode, and SQL is slice 2 (see the SQL follow-up issue).
- A filter-conditioned write with a conditional upsert and a separate slot write per join: not atomic, and it needed a duplicate-key recheck.
- Per-source `$max` slots: they guard the marker, not the data, and grow without bound.
- Optimistic compare-and-retry: needs matched-count handling on every bulk and fenced path.

## Consequences

- A marked read model grows by one small hashed slot per distinct source stream that wrote to it, and the documents carry an `__applied` field that is stripped from every query result like the other sink-owned fields.
- The write path of a marked projection becomes a pipeline update with a guard per field. The budgets are at most +10% for bulk catch-up and +15% for a join fanned out to 1,000 documents, measured with `JoinFanOutProjectionBenchmark` against the base commit. Twice the budget is a stop.
- Other sinks treat the new write mode as `Always`, so they behave as before.
- Projections outside the rule keep the redelivery behavior they had.
- Known limits, both pre-existing and outside this decision. In catch-up, a `Joined` that runs before the own event that sets the reference matches nothing, and the own event's resolved join then reads the latest event before its trigger and can leave stale data; that is engine behavior, not the sink. Clearing markers on partition replay runs an `UpdateMany` on `__applied.j.<definitionId>.s` without an index, a collection scan per join definition; partition replay is rare and the switch is off by default.
