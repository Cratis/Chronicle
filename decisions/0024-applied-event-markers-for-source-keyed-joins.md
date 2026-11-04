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

**The rule.** For a marked projection, behind the existing sink-write guard switch, each document records which source event it last applied per stream: an `f` slot for the document's own source and a `j.<definitionId>` slot for each root join definition. A write carries the condition "this event is newer than the slot", and the slot is updated in the same write. An event that was already applied is therefore a no-op. An event delivered for the first time is applied even when it is older than other writes to the document, because only its own stream's slot decides. Slot names are fixed-length hashes of the stream identity scoped by namespace, never raw source ids.

**Invariant.** A document write for a marked projection is applied if and only if its source event is newer than the slot of its stream on that document. Writes that cannot carry a condition, such as null-parent repairs, record nothing.

**Ordering and failure.** A joined write that spans several segments is cut at every join so each segment records its slot. A guarded upsert rejected with a duplicate `_id` is re-checked as a plain update, never trusted, and that request keeps its identity through the bulk realignment after a failure. The fenced sink probes first, as it does for other guarded writes.

**Partition replay.** A partition replay keeps today's semantics: it rewrites the partition from scratch. So it clears the partition's markers when it starts and again when it resumes after a crash. Clearing is idempotent and global to the sink, so a single call is enough. The partition's own document and the joined documents whose slot belongs to that partition are cleared. Resume does not begin the replay again, because that would notify reactors and reducers twice.

## Rejected alternatives

- Routing partition replay to a full replay: breaks selected-partition semantics and misses persisted jobs.
- A document-wide watermark: drops late first deliveries.
- Observer-level deduplication: the observer cannot know what the sink applied.
- Protecting children, nested, from-every and collapsed writes now: follow-up issue.
- Per-property provenance and raw source ids as field names: size and layout cost without a correctness gain.
- InMemory and SQL: the in-memory sink ignores every write mode, and SQL is slice 2.

## Consequences

- A marked read model grows by one small hashed slot per distinct source stream that wrote to it, and the documents carry an `__applied` field that is stripped from every query result like the other sink-owned fields.
- The write path of a marked projection gets a condition and one extra field. The budgets are at most +10% for bulk catch-up and +15% for a join fanned out to 1,000 documents, measured with `JoinFanOutProjectionBenchmark` against the base commit. Twice the budget is a stop.
- Other sinks treat the new write mode as `Always`, so they behave as before.
- Projections outside the rule keep the redelivery behavior they had.
