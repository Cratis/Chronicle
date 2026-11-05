---
id: 0025-mutation-operation-receipts
title: Mutation-operation receipts
status: accepted
stage: implemented
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
| `DigestVersion` | int: digest algorithm / key version (`1` = domain-separated SHA-256, unkeyed, read only; `2` = HMAC-SHA256 keyed per event store, written) |
| `ArchivedAt` | Unix ms, int64 |

"Through" is `EndExclusive − 1`. The key is (event store, namespace, event sequence, `MutationId`) with a unique (sequence, ordinal). Explicitly absent: payload, command hash, origin, reason, content, event-source id. Redaction targets exist only inside the definition digest.

**Digest.** SHA-256 with domain separation over canonical length-prefixed frames. The definition-digest domain is unchanged. The receipt-digest domain is `…terminal-receipt.v2`; it drops origin and command hash and adds terminal phase and archived-at. Digests are computed behind one internal seam that takes the event store, a digest version and the canonical bytes. The seam separates the write version (the version new digests are written with) from the set of supported read versions. The version is persisted with the definition on the active head and with the receipt (`DigestVersion`), and every recomputation, whether archive, exact retry or begin retry, uses the stored version, so a stored digest is always checked with the algorithm that produced it. A version that is not supported fails closed as corrupt. Version 2, the keyed construction below, is the write version. Receipts and heads written under version 1 stay readable and retry exactly.

**Exact-retry rule.**

1. Recompute the deterministic id; a mismatch is a `DefinitionConflict`.
2. Look up by `MutationId`: active head first, then history.
3. Recompute the definition digest and compare.
4. The result is `Archived` / `AlreadyArchived` with the stored receipt. A different digest is `DefinitionConflict`. A non-matching head state is `StateConflict`.
5. The validator is never weakened.

**Retention.** For the life of the event sequence; no TTL and no sweep (as 0007 §9). If expiry is ever introduced it needs a superseding decision, and a lookup that misses an expired receipt must fail closed with `ReceiptUnavailable`. It must never be treated as a fresh begin.

**Atomicity.** The receipt insert and head release are one provider transaction: MongoDB session transaction (replica set required, as in `production.md`), SQL EF execution strategy plus a database transaction, InMemory one lock.

**Append path.** Never reads or writes receipts; no per-event column.

**Keyed digest (`DigestVersion` 2).** HMAC-SHA256 over the same domain-separated canonical frames as version 1, so the definition-digest and receipt-digest domains are unchanged.

- **Key location.** One secret per event store, in the existing encryption key store, under the reserved identifier `$chronicle-mutation-receipt-digest$v2$` in the default namespace. The secret is per event store, not per namespace.
- **Derivation.** The HMAC key is HKDF-SHA256 over the stored secret, with an empty salt and the fixed info label `cratis.chronicle.mutation-receipt-digest.v2`, 32 bytes. The stored material is never used directly.
- **Creation and concurrency.** The secret is created on first use with the key store's atomic create-if-absent operation (`GetOrAddFor`). It returns the stored key when another silo won the race, so every silo adopts the same secret.
- **Cache.** The derived key is cached in memory per event store, so a mutation does not read the key store. A failure is not cached.
- **Reserved identifier.** The PII and compliance surface refuses any identifier with the reserved prefix. `DeleteEncryptionKeyFor` and `AllowNewEncryptionKeyFor` fail with `EncryptionKeyIsNotErasable` before any storage call, and PII cannot be provisioned under it. Shredding a subject never touches the secret.
- **Fail closed.** The key is resolved before the registry touches any state. If it cannot be read or created, every registry operation returns `Corrupt`. Nothing is written, nothing is re-applied, and no digest falls back to version 1. The key material is never logged or put in an exception.
- **Rotation.** Not implemented. A new secret needs a new identifier and a new `DigestVersion`. The old version stays readable for as long as its secret is kept.

**Residual exposure.** The keyed digest does not meet decision 0007 ruling 3, and this record does not claim compliance with crypto-shredding. The digest key is not erased with a subject's key, so it stays valid after shredding. Anyone who holds both the receipts and the key store (by default the same database) can still confirm an exact guess of a whole definition. An authorized caller who can retry a mutation can do the same online, because an exact retry answers `AlreadyArchived` or `DefinitionConflict`. The key removes offline confirmation by anyone who can read receipts but not the key store. Receipts written under version 1 before the switch keep the plain SHA-256 exposure.

**Relationship to decision 0007.** Decision 0007 ruling 3 refused plain SHA-256 for fingerprints that must not survive crypto-shredding. It also noted that an HMAC keyed per event store is weaker than that requirement, because shredding does not erase the key. Mutation receipts adopt that weaker construction as an explicit, documented exception to ruling 3. A receipt covers a whole mutation definition rather than single protected values, and it must stay comparable after the subject is shredded so that an exact retry is still recognized. 0007's per-protecting-key construction would make a shredded mutation impossible to retry exactly. Producers carry the remaining constraint below.

**Q1 (RESOLVED, option B).** Q1 asked whether receipts use plain domain-separated SHA-256 (A, an explicit exception to 0007 ruling 3) or HMAC-SHA256 keyed per event store (B). B was taken on 2026-11-05 as the stated default after the coordinator did not answer by 06:00; an independent second opinion recommended it as well. A remains as the read-only `DigestVersion` 1.

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
| Plain domain-separated SHA-256 as the write version (Q1, option A) | Rejected: anyone who can read a receipt could confirm a guess offline. Kept as the read-only `DigestVersion` 1. |
| Keyed digest (HMAC per event store) | Taken (Q1, option B) as `DigestVersion` 2, with the key management above. |
| Per-protecting-key fingerprints as in 0007 section 4 | Rejected: a shredded mutation could no longer be retried exactly. |

## Default if unanswered

Not applicable: Q1 is resolved as option B (see above).

## Timeline and scope

D1: the receipt-shaped registry, schema and migration `v19_127_0`. D2: provider-atomic archive and the 5-provider retry conformance. D2b: the keyed `DigestVersion` 2 and its per-event-store key. Out of scope: producers (#3910), key rotation, any append-path change, receipt expiry.

## Verification

Done when: the registry, the migration and the receipt shape exist in all providers; Release build with warnings as errors, the Concepts, Storage, InMemory, MongoDB, Core and SQLite spec projects pass; the seven retry-conformance facts pass on InMemory, MongoDB (replica set) and SQLite and are listed for PostgreSQL and SQL Server in CI.

Verify by: the history persisted-structure spec; the digest-version conformance on every provider (version 1 receipts retry as `AlreadyArchived` after the switch, version 2 receipts round-trip); the key-convergence, fail-closed and PII-guard specs; the append-path diff against origin/main is empty; no logger call in the Mutations folders includes payload or command hash.

## Consequences

Easier: exact retries are safe after head reuse; receipts are small and uniform. Harder: Mongo archive needs a replica set; transient transaction conflicts surface as `StateConflict` after one retry and the caller retries exactly. Foreclosed: per-event mutation markers and receipt expiry without a new record.
