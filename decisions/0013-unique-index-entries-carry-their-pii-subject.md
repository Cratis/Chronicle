---
id: 0013-unique-index-entries-carry-their-pii-subject
title: A unique-index entry derived from a [PII] value records its subject, and erasing the subject's key deletes those entries
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-19
decider: woksin
applies-to:
  - Source/Kernel/Core/Events/Constraints/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Compliance/GDPR/PIIManager.cs
  - Source/Kernel/Core/Patches/19.101.0/**
  - Source/Kernel/Storage/Events/Constraints/**
  - Source/Kernel/Storage.InMemory/Events/Constraints/**
  - Source/Kernel/Storage.MongoDB/Events/Constraints/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/UniqueConstraints/**
---

# A unique-index entry derived from a [PII] value records its subject, and erasing the subject's key deletes those entries

Tracking issue: [#4389](https://github.com/Cratis/Chronicle/issues/4389). Builds on
[decision 0008](0008-repair-constraint-indexes-from-the-log.md), which treats a new persisted index field as a
decision of its own.

## Context

A `[Unique]` constraint over a `[PII]` property stores an unsalted SHA-256 of the plaintext. Erasing a subject
destroys the encryption key, so the event can no longer be read, but the index entry stays. The value stays
"taken" forever, and its hash confirms a guess at the erased value.

Erasure cannot find these entries. An entry holds the event source, the hash and the claiming sequence number. The
subject is not the event source when `[Subject]` names someone else (a company stream holding a contact's e-mail),
and the event store cannot be queried by subject.

## Decision

1. **The entry records its subject, only for personal data.** When the unique index is written and at least one
   property the constraint covers for that event type carries compliance metadata in the event's schema generation,
   the entry stores the event's subject (`EventContext.Subject`, or the event source id when no subject is set).
   Every other entry stores no subject. Erasure therefore never frees an entry that holds no personal data.
2. **Every writer records it.** The append, the effect-receipt completion, the 0008 replay and fold, and the full
   reindex all write the subject. A repair that replaces an entry keeps its subject.
3. **Erasure deletes by subject.** `PIIManager.DeleteEncryptionKeyFor` deletes, after the erasure fence and the key
   deletion, every entry recording the subject. It does this in every unique-index table or collection of every
   event sequence in the key's namespace, across the event stores it already reaches. A failed delete joins the
   existing failures, so the call reports `EncryptionKeyErasureIncomplete` and can be re-run; the delete is
   idempotent.
4. **Storage.** MongoDB adds a `subject` field and a sparse `subject` index to both collection shapes. SQL adds a
   nullable `Subject` column, with an index, to both table shapes. Existing tables gain the column through the
   table migrator's upgrade path, which only adds the column if it is missing. The in-memory store keeps the
   subject with each value.
5. **Entries written before this release are backfilled by a reindex.** Patch `19.101.0`
   (`RecordSubjectsOnUniqueConstraintClaims`) starts the 0008 reindex job, in every namespace of every event store,
   for each unique constraint that covers a personal-data property in any generation of its event types. It
   re-derives each entry from the event log with its subject. Constraints without personal data are left alone; the
   keyed-hash step reindexes every unique constraint later. Decided by the
   decider on 2026-10-19, with both of its visible effects accepted and stated in the release note:
   - values held by subjects erased **before** this release become claimable again, because the reindex skips
     erased subjects' events;
   - governed appends to a constraint are refused while that constraint rebuilds (the 0008 pending-index state).
6. **Release:** minor.

## Options considered

- **Record the subject on the entry (chosen).** One indexed delete per table at erasure time.
- **Reindex on every erasure.** No new field, but each erasure replays the log and refuses governed appends while
  it runs. Rejected.
- **Check each entry's claiming event at erasure time.** No new field, but one event read per entry on every
  erasure. Kept as the fallback for backfilling legacy entries.
- **Salt the hash per subject (the issue's suggestion).** Rejected by the decider: two subjects with the same value
  would no longer collide, which breaks uniqueness.

## Consequences

- An erased subject's values become available again as soon as the erasure succeeds. This is intended.
- Entries written before this release gain their subject when the `19.101.0` backfill reindex completes; until it
  does, erasure cannot find them. Values of subjects erased before this release become claimable again then.
- While the backfill rebuilds a constraint, governed appends to it are refused.
- An append that obtained the subject's key before the erasure fence, and writes its index entry after the
  erasure's delete, leaves an entry behind. Re-running the erasure removes it.
- The unsalted hash still confirms guesses against entries that have not been erased. Replacing it with a keyed
  HMAC is step 2 of #4389.

## Verification

Specs in every storage provider cover recording the subject only for personal data, and deleting by subject while
keeping other subjects' entries. A `PIIManager` spec covers the erasure failing closed when the delete fails. A
patch spec covers the backfill reindexing only the unique constraints that cover personal data.
