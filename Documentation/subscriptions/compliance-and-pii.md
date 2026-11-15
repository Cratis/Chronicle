---
title: Chronicle subscriptions and PII protection
description: Chronicle preserves subject identity when forwarding events and copies missing encryption keys without restoring keys for erased subjects.
---
Cratis Chronicle preserves an event's subject when a subscription forwards it to another event store. For personally identifiable information (PII), Chronicle also copies the subject's encryption key if the target has no key and permits one. This keeps forwarding tied to the subject's identity without restoring a key after erasure.

## What is preserved during forwarding

The forwarded event retains `EventContext.Subject`. Chronicle derives the encryption key identifier from that subject in both stores, so the target protects personal data under the same subject identity.

Key storage is scoped to an event store and namespace. Forwarding can therefore leave a subject's key in more than one store, even if your application only appended that subject's events to the source. Erasure must reach those copies too.

## Forwarding behavior

When an outbox event is forwarded to an inbox:

1. Chronicle uses the event subject as the compliance identity.
2. Chronicle checks whether the target store namespace already has a key for that identity.
3. If the target key is missing, Chronicle checks the subject's erasure record. If erasure blocks a new key, no key is copied and the refusal is logged.
4. Otherwise, if the source key exists, Chronicle copies it to the target store namespace and logs the copy. An existing target key is kept.
5. Chronicle appends the event to the inbox with the original subject preserved.

The forwarding subscriber receives events with protected values already decrypted. The inbox append encrypts each `[PII]` value again using the target's key for that subject. Copying supplies the source key when the target lacks one; it does not replace a key the target already holds or make the inbox append bypass PII protection.

## Why this matters for erasure

Erasing a subject through one event store attempts erasure across every event store in that namespace. Chronicle records that new keys are blocked before deleting the keys, so forwarding cannot copy a surviving key back into an erased store. A partial failure is reported rather than treated as completed erasure; follow [Erasing a subject](../compliance/erasing-a-subject) to handle that result.

This scope matters when deciding where to erase: request erasure once per namespace, not separately for each subscription. Data your consumers copied into read models or external systems still needs its own handling.

> [!IMPORTANT]
> A forwarded event that carries `[PII]` for an erased subject cannot be appended in the target event store while new keys are blocked. The observer partition for that event source enters a failed state; other partitions can continue. Stop producing that subject's personal data, or deliberately authorize a new key with `AllowNewEncryptionKeyFor`. Authorization permits future protected values; it does not recover the erased key or old personal data.

## Observing the copy

Chronicle logs each copy at debug level from the forwarding subscriber, naming both event stores and the namespace:

```text
Copied a subject's encryption key from event store 'Sales' to event store 'Support' in
namespace 'Default' while forwarding events. That subject now holds a key in both event
stores, and an erasure reaches every event store in the namespace
```

A skipped copy is logged at information level, because it means personal data is still being forwarded for someone who was erased:

```text
Did not copy a subject's encryption key from event store 'Sales' to event store 'Support' in
namespace 'Default' while forwarding events, because that subject was erased in the target
```

Neither message includes the subject. Logging that identity would create an unencrypted copy of personal data outside the protection of the deleted key. Use the store names and namespace to trace where key propagation occurred.

## Practical guidance

Set an explicit, stable subject for events that carry personal data, such as the identifier of the person the data belongs to. The subject connects protection and erasure across the stores that receive those events.

Before erasing a subject, stop publishing their personal data or plan for the affected forwarding partitions to fail. If forwarding has already failed, use [Subscription recovery](recovery.mdx) to distinguish the source forwarding observer from the consumer's inbox observers before retrying or replaying.

## See also

- [Outbox and Inbox](outbox-inbox)
- [Implicit Event Store Subscriptions](implicit-subscriptions)
- [Explicit Event Store Subscriptions](explicit-subscriptions)
- [Erasing a subject](../compliance/erasing-a-subject)
- [The encryption key lifecycle](../compliance/key-lifecycle)
- [Compliance](../compliance/index.md)
