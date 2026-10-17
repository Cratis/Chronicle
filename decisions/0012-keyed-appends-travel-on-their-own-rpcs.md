---
id: 0012-keyed-appends-travel-on-their-own-rpcs
title: Keyed appends travel on their own RPCs, a key hit marks the constraint gap without other side work, and keyed storage reports a proven non-commit the way decision 0008 does
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-17
decider: Sindre Wilting (delegated to L0)
supersedes: 0007-idempotent-keyed-appends
applies-to:
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Kernel/Grpc/Clients/ConnectionService.cs
  - Source/Clients/DotNET/EventSequences/**
  - Documentation/events/appending.mdx
  - Documentation/events/appending-many.mdx
  - Documentation/clients/where-the-clients-differ.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Keyed appends travel on their own RPCs, a key hit marks the constraint gap without other side work, and keyed storage reports a proven non-commit the way decision 0008 does

## Relation to decision 0007

This record replaces [Decision 0007](0007-idempotent-keyed-appends.md) by carrying it forward with four changes. **Every part of 0007 that this record does not change stays in force, as part of this decision, word for word.** That includes the key shape, the fingerprint and crypto-shredding comparison, the outcomes, per-event settlement of batches, replay and retention, and the analyzers. 0007 is marked superseded only so that the record a reader lands on points here. Read 0007 for the whole design and this record for what changed.

The four changes come from the implementation plan for Cratis/Chronicle#4809 (decision 0007's build work), which found them after [Decision 0008](0008-repair-constraint-indexes-from-the-log.md) was implemented in Cratis/Chronicle#4895 and reworked the same append and storage code.

## Context

1. **Older kernels drop the key without saying so.** 0007 section 2 adds the key as a new field at the end of the existing `Append` and `AppendMany` request messages. Proto3 ignores fields it does not know, so a kernel from before this work appends the event *without* the key and reports success. 0007 closes this only on the .NET client, which checks the `idempotent-appends` capability before sending. The Kotlin, TypeScript, Elixir and Python clients get the field from regenerated contracts but have no such check. A gRPC channel behind one load-balanced address can also reconnect to an older pod during a rolling upgrade without checking compatibility again. In both cases the key is silently lost, and with it the deduplication the caller relies on.
2. **0007 and 0008 disagree about a key hit.** 0007 section 3 says a keyed hit does no side work: it returns the stored receipt and enqueues nothing. 0008 treats a durable event whose index work may be missing as a gap below the watermark, and its publication reconciliation folds and enqueues the stored event. Both cannot hold for the same hit.
3. **0007 and 0008 report a failed storage commit differently.** 0007 section 3 has storage return `TransactionConflicted` as a failure value when a MongoDB transaction conflicts. 0008 has storage throw `EventsNotCommitted` when the provider proved nothing from the attempted append was committed, and the grain distinguishes that from an unknown outcome.
4. **The release label for the kernel work** was not settled for the kernel projects, which are not packaged.

## Decision

### 1. Keyed appends use new RPCs (replaces 0007 section 2, "Core artifacts")

- Keyed appends travel on **new** RPCs, added as new Core command artifacts:
  - `AppendOnce`, a single event carrying its idempotency key;
  - `AppendManyOnce`, a batch carrying one key per event, aligned with the events.

  Each one also gets a named-tags variant where the existing append has one.
- An older kernel has no such RPC and answers `Unimplemented`. A client in any language therefore fails closed: the call is refused, and the key is never dropped.
- The existing `Append`, `AppendWithNamedTags`, `AppendMany` and named-tags RPCs and their messages stay **unchanged**. No idempotency field is added to them, and no existing field number moves.
- The new RPCs use the result shapes 0007 section 2 defines: `Outcome` or `Outcomes`, `IdempotencyConflicts`, and the error codes in the existing `Errors` field.
- `KernelCapabilities.IdempotentAppends` is still advertised, so a client can choose before calling. It is no longer what keeps a key from being dropped; the RPC itself does that.
- The .NET client keeps 0007's API: `AppendOnce`, `AppendManyOnce`, and a key on `EventForEventSourceId`. It sends these calls over the new RPCs. An `EventForEventSourceId` batch in which any event carries a key goes over `AppendManyOnce`. The client's capability check before sending stays as a clearer error than `Unimplemented`.
- 0007's grain methods (`AppendWithIdempotencyKey`, `AppendManyWithIdempotencyKeys`) and their mixed-version reasoning are unchanged.

### 2. A key hit does no side work (refines 0007 section 3 against 0008)

- When a keyed append finds its key durable, the grain returns the stored receipt and:
  - folds nothing;
  - enqueues nothing to observers;
  - provisions no encryption key.

  This is what 0007 says, and it wins over 0008's publication reconciliation for keyed hits.
- The hit does **mark the gap**. If the stored event's sequence number is at or above the constraint index watermark, the grain records it as needing index work, exactly as 0008 records any gap. The background catch-up and the repair before the next validation then apply its claims and closures from the log. The hit itself never applies them from the retry's content.
- A keyed hit still raises the grain's next sequence number and per-type tail, as 0007 section 3 describes.
- Specifications cover both behaviors: a keyed hit after a turn that failed after the write enqueues nothing and folds nothing, and it leaves the event marked so the constraint index catches up from the log.

### 3. One failure shape per storage method, following 0008 (replaces `TransactionConflicted` in 0007 sections 3 and 7)

- Each storage method reports a failed commit in exactly one way. The new keyed storage method `AppendManyWithIdentities` follows 0008:
  - a provider that proves nothing from the attempt was committed throws `EventsNotCommitted`, which on MongoDB includes a `WriteConflict` with another uncommitted transaction;
  - a slot collision returns `DuplicateEventSequenceNumber`;
  - a unique-identity collision returns `IdentitiesAlreadyDurable` with the receipts found.
- `TransactionConflicted` is not introduced.
- The grain treats `EventsNotCommitted` from a keyed append exactly as 0007 treats `TransactionConflicted`:
  1. it looks the keys up again;
  2. it gives every key that is now durable its outcome;
  3. it retries the keys that are still absent, once;
  4. after a second report it settles again without writing, and fails any key still absent with `IdempotencyKeyUnsettled`.
- An outcome that remains unknown after storage reports it (0008's `AppendOutcomeUnknown` path) is settled for a keyed append by looking the keys up, not by reading the content back. A key found durable gets its 0007 outcome; a key still absent is reported as `AppendOutcomeUnknown`.

### 4. Release label

The kernel settlement work, the wire work, and the .NET client work each ship as **minor**: additive RPCs and client API, nothing removed. Storage-only steps that change no public surface may ship as patch, as 0007's timeline says.

## Options considered

- **For the wire: keep the key as a field on the existing requests** (0007 as written), and document that clients must check `idempotent-appends` first. Rejected: every non-.NET client and every reconnect behind a load-balanced address would silently lose the key, and correctness would depend on each client author reading the documentation. The effect-receipt field accepted the same exposure, but a dropped receipt is visible, while a dropped key appends a duplicate and reports success.
- **For the wire: new RPCs** (chosen). This costs a second pair of append endpoints in every client. In exchange, an older kernel refuses the call.
- **For a key hit: fold and enqueue as 0008's reconciliation does.** Rejected: it rebuilds index work from the retry's content and re-delivers an event that observers may already have read from storage, which is the side work 0007 rules out.
- **For a key hit: neither mark the gap nor fold.** Rejected: an event whose turn failed after the write would keep a missing claim until some unrelated repair noticed it, which is the drift 0008 exists to stop.
- **For storage failures: keep both `TransactionConflicted` and `EventsNotCommitted`.** Rejected: two shapes for the same proven non-commit invite a caller that handles one and misses the other.

## Default if unanswered

0007 would be built as written: the key travels as a field on the existing requests, a hit both does no side work (0007) and gets folded (0008, contradiction left to the implementer), and storage reports conflicts in two shapes. The cost is silently dropped keys for every non-.NET client and an unresolved contradiction in the append path.

## Timeline and scope

- **Holds for:** the lifetime of keyed appends in Chronicle 19.x and later, until superseded.
- **In scope:** the four changes above, for the kernel, the storage providers, the wire contract and the .NET client.
- **Out of scope:** keyed appends through the transactional unit-of-work commit path, which 0007 also leaves out. Clients other than .NET get the new RPCs from their regenerated contracts; adding their own keyed-append API is separate work.
- **Delivery:** PRs D (kernel settlement) and E (wire and capability) of the #4809 implementation plan carry these changes. Storage PRs A to C proceed as 0007 describes, using the failure shape in section 3.

## Verification

- **Done when:**
  - an older kernel answers a keyed append with `Unimplemented` and appends nothing;
  - the existing append RPCs and messages are byte-for-byte unchanged in the generated contract;
  - a keyed hit enqueues nothing, folds nothing, and leaves its event marked for constraint index catch-up;
  - `AppendManyWithIdentities` throws `EventsNotCommitted` for a MongoDB write conflict and never returns `TransactionConflicted`.
- **Verify by:**
  - the `wire-compatibility.yml` gates (stale check, every released minor still served), plus a `Compatibility.Specs` spec that a client with the new RPCs is compatible with the previous release's descriptor set;
  - a client integration spec against a kernel without the new RPCs;
  - Core.Specs interleaving specs for the keyed hit (no enqueue, no fold, gap marked);
  - `Storage.MongoDB.Specs` with two concurrent keyed transactions.

## Consequences

- Two append endpoints exist per client: keyed and unkeyed. Documentation names the keyed ones as the way to make a retry safe, and `where-the-clients-differ.md` lists which clients expose them.
- A keyed append against an older kernel fails loudly instead of degrading to an unkeyed append.
- 0008 owns all index work for keyed events too. A keyed hit only marks the gap.
