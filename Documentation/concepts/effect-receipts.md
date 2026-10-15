---
title: Effect receipts
description: Deduplicate an occurrence's Chronicle effects and recover the recorded outcome after a lost acknowledgment.
---

A worker can commit events and disappear before acknowledging its work. Running it again may produce different events or a different response. An **effect receipt** lets you recover the first committed outcome instead of appending another set of events.

Use receipts for retryable work identified by an occurrence and a nonempty effect name. They live in infrastructure storage, not in the event log: projections and reactors never observe a receipt.

## Trigger payload contracts

A trigger names a kind of retryable work. Register its stable identity, display name and JSON payload schema through the kernel's `RegisterTriggers` command. Definitions belong to the event store, rather than to an individual namespace. Repeating the same registration preserves its revision; changing the contract creates a new revision, so an occurrence can retain the revision it was validated against.

Trigger payloads are plaintext infrastructure data, not events. A schema carrying compliance metadata at any nesting depth is refused with `TriggerSchemaCarriesCompliance`. The kernel validates the whole registration request before saving definitions, so this refusal does not leave the request's other definitions partially registered. Registration and delivery are separate from effect receipts; a receipt alone does not record or deliver an occurrence.

## Record and deliver an occurrence

With the kernel's `occurrences` capability, `RecordOccurrence` validates an application, startup or shutdown payload against the trigger's current schema, then inserts its identity before replying. The identity combines the trigger and your idempotency id. Repeating it with the same canonical JSON returns `AlreadyRecorded`; a different payload returns `OccurrenceIdConflict`. Invalid payloads return `OccurrencePayloadInvalid` and write nothing. Keep the same idempotency id after a lost acknowledgment.

Queues belong to an event store and namespace. Select `ApplicationScope` explicitly for one queue shared by the application, independent of namespace. Application-scoped effects still target individual namespaces, and their receipts remain in those namespaces. Scheduled materialization and lifecycle fan-out are separate capabilities, not part of this ingress API.

`ClaimOccurrences` returns a bounded batch with each occurrence's payload, attempt and fencing epoch. Use the client's connection identity as `WorkerId` so disconnect expiry can find its leases. The default lease lasts 60 seconds; requested durations are capped at ten minutes. `RenewOccurrenceLease` extends ownership from the kernel's current time, never the worker's clock.

`CompleteOccurrence` and `FailOccurrence` carry the occurrence id and claimed epoch. A superseded epoch returns `OccurrenceLeaseLost` and changes nothing. An expired lease can still complete or renew if no replacement claimed it. Repeating a successful completion at the same epoch returns `AlreadyCompleted`.

## Retry without blocking later work

A claim increments the attempt count. A failure releases the lease and sets an exponential retry delay, initially one second, with a factor of two and a ten-minute cap. An expired lease is claimable by another worker and its replacement claim counts another attempt. Once the configured attempt limit is exhausted, failed or reclaimed work is quarantined instead of starting another attempt.

By default, a trigger has only one live lease. Claims follow recording order, but a retrying occurrence waits out its backoff without blocking later work. A later occurrence can therefore complete first. If your handler needs stronger ordering, derive it from application state rather than treating queue order as a business invariant.

Quarantine parks only the affected occurrence. After correcting the cause, an operator calls `RetryQuarantinedOccurrence`. This explicitly authorized command atomically resets attempts and records the authenticated operator and kernel time; it preserves the epoch so old lease tokens cannot become current again. Configure retry limits and concurrency under [Occurrences](../configuration/chronicle-options.mdx#occurrences).

## Inspect and recover delivery

`GetOccurrence` returns delivery state, attempts, epoch, owner, expiry, last error, next attempt and effect receipts. Application-scoped lookup gathers receipts from the store's namespaces. These reads are status snapshots: occurrence state and receipts do not share a transaction.

`ObserveOccurrences` supplies an initial snapshot and periodic durable snapshots, including when time alone makes a retry claimable. Workers can use it as a wake signal, then claim through the queue; a notification never grants ownership. Dispose the subscription when stopping a worker.

An active queue uses a grain timer and a persistent reminder with a one-minute period. Startup also rehydrates unfinished queues, including application scope and namespaces with no event-log data. Storage decides claimability after every wake-up. InMemory loses its queues with the process, just as it loses events and receipts.

A recorded shutdown occurrence is durable work. A process that dies before recording shutdown creates no occurrence: record essential cleanup intent while the application is running, not only during shutdown.

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

This command always uses the Log grain. If it finds a receipt for an effect written to another sequence, it returns that receipt without reconciling the other sequence's grain state. Retry the original append on its destination sequence when that reconciliation is needed.

A no-event decision with protected reads must instead use `AppendManyWithEffectReceipt` with **zero events and its concurrency scopes**. The grain validates those scopes before committing a `NoEvents` receipt in the batch transaction. A violated scope writes no receipt. Receipt-only writes cannot bypass protected reads.

## Keep the response small and nonpersonal

The optional response is an opaque JSON value, returned verbatim, with a maximum UTF-8 size of **64 KiB**. It can carry the result needed to answer a duplicate command without rerunning its handler. Invalid JSON and larger responses are refused before writing.

Responses are plaintext and are not crypto-shredded. Do not record personal data or credentials. Prefer a small nonpersonal result or reference rather than a copy of an entity.

## What a receipt does not guarantee

A receipt deduplicates Chronicle effects; it does not fence the effect against a superseded lease, and it is not atomic with occurrence completion. Redelivery reconciles those separate writes.

It also cannot make an email, payment or other external operation exactly once. Give the external endpoint a stable idempotency key and follow its own recovery contract. Ordinary appends that do not represent retryable occurrence work do not need receipts.

MongoDB requires the replica set already used by transactional batches. SQL commits events and receipts together. InMemory keeps them atomic within the process, but both disappear when the process ends.

The endpoints follow the kernel's fallback authorization policy: authentication is required when enabled, with no additional per-trigger roles. For the consistency checks that still apply to a receipt miss, see [event concurrency](../events/concurrency.mdx).
