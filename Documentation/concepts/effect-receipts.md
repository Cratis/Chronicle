---
title: Effect receipts
description: Deduplicate an occurrence's Chronicle effects and recover the recorded outcome after a lost acknowledgment.
---

A worker can commit events and disappear before acknowledging its work. Running it again may produce different events or a different response. An **effect receipt** lets you recover the first committed outcome instead of appending another set of events.

Use receipts for retryable work identified by an occurrence and a nonempty effect name. They live in infrastructure storage, not in the event log: projections and reactors never observe a receipt.

## One key, one winning outcome

A receipt belongs to an event store and namespace. Its identity combines the occurrence id and effect key; it does not include the retry's payload or lease epoch. The first writer wins, even if another attempt proposes different events or a different response.

The receipt records one of three outcomes:

| Outcome | What it records |
| --- | --- |
| `Appended` | The event sequence and inclusive first and last sequence numbers |
| `NoEvents` | A completed effect that appended nothing |
| `Refused` | An explicit business refusal and its reasons |

Each outcome also carries the writing lease epoch, recording time, and optional response. `GetEffectReceipt` looks up that winning outcome without executing the effect. Receipts are retained for the life of the namespace.

## Commit the receipt with the events

The kernel's `AppendManyWithEffectReceipt` grain method looks up the receipt before validating the proposed batch. A hit returns `EffectAlreadyRecorded` with the stored outcome and response, and appends nothing. It also completes post-commit bookkeeping from the stored events, including observer dispatch and constraint indexes, if the original acknowledgment was lost.

On a miss, the normal schema, constraint and concurrency pipeline runs. The events and receipt then commit in one storage transaction. A pipeline refusal writes no receipt. A competing writer is resolved by looking up the winning receipt again; `EffectReceiptUnsettled` is a retryable failure, not a business refusal. After an unknown acknowledgment, look up the receipt or retry with the same effect key.

This lookup-before-validation guarantee belongs to the **grain**. HTTP and gRPC append facades retain their request validators and conversions. A malformed request can therefore be refused before reaching the grain, even if its effect already has a receipt; that refusal writes nothing. Use the lookup query when you only need the recorded outcome.

`EffectReceipt` is an optional final field on each append command, including the named-tag variants. Send it only when the kernel advertises `effect-receipts`: an older kernel could otherwise ignore the field and append without deduplication. These are kernel and wire surfaces; client enrollment is a separate integration.

## An effect without events

`RecordEffectReceipt` accepts `NoEvents` or `Refused` without concurrency scopes. It returns `Recorded` for a new receipt or `AlreadyRecorded` with the first writer's receipt. Record an explicit terminal business refusal here; do not turn a transient storage failure or concurrency conflict into a terminal receipt.

A no-event decision with protected reads must instead use `AppendManyWithEffectReceipt` with **zero events and its concurrency scopes**. The grain validates those scopes before committing a `NoEvents` receipt in the batch transaction. A violated scope writes no receipt. Receipt-only writes cannot bypass protected reads.

## Keep the response small and nonpersonal

The optional response is an opaque JSON value, returned verbatim, with a maximum UTF-8 size of **64 KiB**. It can carry the result needed to answer a duplicate command without rerunning its handler. Invalid JSON and larger responses are refused before writing.

Responses are plaintext and are not crypto-shredded. Do not record personal data or credentials. Prefer a small nonpersonal result or reference rather than a copy of an entity.

## What a receipt does not guarantee

A receipt deduplicates Chronicle effects; it does not fence the effect against a superseded lease, and it is not atomic with occurrence completion. Redelivery reconciles those separate writes.

It also cannot make an email, payment or other external operation exactly once. Give the external endpoint a stable idempotency key and follow its own recovery contract. Ordinary appends that do not represent retryable occurrence work do not need receipts.

MongoDB requires the replica set already used by transactional batches. SQL commits events and receipts together. InMemory keeps them atomic within the process, but both disappear when the process ends.

The endpoints follow the kernel's fallback authorization policy: authentication is required when enabled, with no additional per-trigger roles. For the consistency checks that still apply to a receipt miss, see [event concurrency](../events/concurrency.mdx).
