---
id: 0025-mutation-operation-receipts
title: Mutation-operation receipts
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-05
decider: L0 (woksin's delegation)
applies-to:
  - Source/Kernel/Concepts/EventSequences/Mutations/**
  - Source/Kernel/Concepts.Specs/EventSequences/Mutations/**
  - Source/Kernel/Storage/EventSequences/Mutations/**
  - Source/Kernel/Storage.Specs/EventSequences/Mutations/**
  - Source/Kernel/Storage.InMemory/EventSequences/Mutations/**
  - Source/Kernel/Storage.InMemory.Specs/EventSequences/Mutations/**
  - Source/Kernel/Storage.MongoDB/EventSequences/Mutations/**
  - Source/Kernel/Storage.MongoDB.Specs/EventSequences/Mutations/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/Mutations/**
  - Source/Kernel/Storage.Sql.Specs/EventStores/Namespaces/EventSequences/Mutations/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Mutation-operation receipts

Accepted on 2026-11-05 by L0 on woksin's delegation for Cratis/Chronicle#3920, under ruling chronicle-rulings-20261105 §1.

## Context

Content mutations of an event sequence (redaction and revision) are begun, driven to a terminal phase and then archived so the active head can be reused for the next mutation. A caller that crashed or timed out must be able to retry the same mutation exactly, even after its head has been archived and reused, and get back the original outcome. Epoch fencing (#3937) protects the sequence but records nothing about completed operations. Without a durable terminal record, an exact retry after head reuse is indistinguishable from a fresh begin and would run the mutation twice.

Decision 0007 ruling 3 refused plain SHA-256 over plaintext for fingerprints that must not survive crypto-shredding. The 2026-11-05 ruling accepts a definition digest that covers redaction targets, payload and reason, with the exposure documented here.

## Decision

Every archived mutation leaves one payload-free, fixed-size receipt in a history store, written in the same provider transaction that releases the active head and kept for the life of the event sequence. An exact retry recomputes the deterministic id, looks up the active head and then the history, compares the definition digest and gets the original receipt back (`AlreadyArchived`), a typed conflict, or `StateConflict`. The append path never reads or writes receipts.

| Field | Type |
|---|---|
| `MutationId` | UUIDv8 over (target, origin, originSeqNo, kind) |
| `Kind` | enum |
| `Ordinal` | uint64 |
| `DefinitionDigestV1` | 32 bytes / 64 hex |
| `Target.Start`, `Target.EndExclusive`, `Target.ExpectedCount` | uint64 |
| `TerminalPhase` | enum, always `SourceCommitted` |
| `FinalStateVersion` | int64 |
| `RepairState` | enum |
| `ReceiptDigestV1` | 32 bytes |
| `ArchivedAt` | Unix ms, int64 |

"Through" is `EndExclusive − 1`. The key is (event store, namespace, event sequence, `MutationId`) with a unique (sequence, ordinal). Explicitly absent: payload, command hash, origin, reason, content, event-source id. Redaction targets exist only inside the definition digest.

**Digest.** SHA-256 with domain separation over canonical length-prefixed frames. The definition-digest domain is unchanged. The receipt-digest domain is `…terminal-receipt.v2`; it drops origin and command hash and adds terminal phase and archived-at. The digest is computed behind one internal seam (the digest calculator) so a keyed construction can replace it without touching callers.

**Exact-retry rule.**

1. Recompute the deterministic id; a mismatch is a `DefinitionConflict`.
2. Look up by `MutationId`: active head first, then history.
3. Recompute the definition digest and compare.
4. The result is `Archived` / `AlreadyArchived` with the stored receipt. A different digest is `DefinitionConflict`. A non-matching head state is `StateConflict`.
5. The validator is never weakened.

**Retention.** For the life of the event sequence; no TTL and no sweep (as 0007 §9). If expiry is ever introduced it needs a superseding decision, and a lookup that misses an expired receipt must fail closed with `ReceiptUnavailable`. It must never be treated as a fresh begin.

**Atomicity.** The receipt insert and head release are one provider transaction: MongoDB session transaction (replica set required, as in `production.md`), SQL EF execution strategy plus a database transaction, InMemory one lock.

**Append path.** Never reads or writes receipts; no per-event column.

**Residual exposure.** Plain SHA-256 over the definition lets anyone who can read a receipt after shredding confirm an exact guess of the whole definition. This is acceptable only because a definition has high entropy. Open question Q1 (keyed digest) is with the coordinator; until answered, this is the construction.

**Producer constraint for #3910.** Producers must not put low-entropy secrets as the sole variable in a mutation definition. `ActiveCommandPayload` is plaintext while a head is active (an abandoned head keeps it until archived) and is cleared inside the archive transaction.

## Options considered

| Option | Verdict |
|---|---|
| Restore the registry from 911558cd1f with deltas | Taken: already reviewed in #4009's branch. |
| Port #3929 | Rejected: older, fails 3 of 7 retry conformance facts and relies on head residue reconstruction. |
| Rebuild on main | Rejected: re-derives about 6k production and 6k spec lines. |
| Keep Origin and CommandHash on the receipt | Rejected: variable-length and caller-supplied; violates fixed-size, digest-only. Both stay bound through the id and the definition digest. |
| Insert receipt then release head with crash recovery | Rejected: not provider-atomic, needs residue reasoning. |
| `LastMutationOrdinal` on every event row | Rejected: on the append path and unused by the registry. |
| Keyed digest (HMAC per event store) | Not taken: needs key management; see Default. |

## Default if unanswered

Plain domain-separated SHA-256 as ruled, with the residual exposure above documented. If a keyed digest is later required, the receipt-digest domain bumps and the seam is replaced; no caller changes.

## Timeline and scope

D1: the receipt-shaped registry, schema and migration `v19_127_0`. D2: provider-atomic archive and the 5-provider retry conformance. Out of scope: producers (#3910), key management, any append-path change, receipt expiry.

## Verification

Done when: the registry, the migration and the receipt shape exist in all providers; Release build with warnings as errors, the Concepts, Storage, InMemory, MongoDB, Core and SQLite spec projects pass; the seven retry-conformance facts pass on InMemory, MongoDB (replica set) and SQLite and are listed for PostgreSQL and SQL Server in CI.

Verify by: the history persisted-structure spec; the append-path diff against origin/main is empty; no logger call in the Mutations folders includes payload or command hash.

## Consequences

Easier: exact retries are safe after head reuse; receipts are small and uniform. Harder: Mongo archive needs a replica set; transient transaction conflicts surface as `StateConflict` after one retry and the caller retries exactly. Foreclosed: per-event mutation markers and receipt expiry without a new record.
