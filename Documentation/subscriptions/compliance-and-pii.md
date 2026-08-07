# Compliance and PII

When subscriptions forward events from a source event store outbox to a target event store inbox, Chronicle preserves the compliance identity of every event — and copies the subject's encryption key along with it. The first half is what you would expect. The second half has consequences for right-to-erasure that are worth understanding before you rely on it.

## What is preserved during forwarding

For each forwarded event, Chronicle preserves:

- **Subject** from `EventContext.Subject`
- **Encryption key identity** derived from that subject
- **Encryption key availability** in the target event store namespace when missing

The subject travels with the event, so a person's data is keyed to the same identity in both event stores and every event that touches them is protected by the same compliance decisions.

## Forwarding behavior

When an outbox event is forwarded to an inbox:

1. Chronicle uses the event subject as the compliance identity.
2. Chronicle checks whether the target store namespace already has a key for that identity.
3. If the target key is missing and the source key exists, Chronicle copies the key to the target store namespace and logs that it did so.
4. Chronicle appends the event to the inbox with the original subject preserved.

Step 3 is what makes the two event stores share one key pair per subject. It is not what makes the forwarded payload readable: the forwarding subscriber receives events already decrypted, and the append in step 4 re-encrypts every `[PII]` value under whatever key the target store holds for that subject — minting one if there is none. Forwarding works either way; the copy is what keeps the *same* key on both sides.

## Why this matters for erasure

Erasure in Chronicle is the deletion of an encryption key, and `IEventStore.PII.DeleteEncryptionKeyFor` deletes it for exactly one `(event store, namespace)` pair. Once forwarding has copied a subject's key, that subject has a key in more than one pair — so:

- **Erasing through one event store leaves the other copy readable.** Nothing reports the incomplete erasure; the delete succeeds.
- **A surviving copy can restore a deleted key.** Because step 2 copies only *when missing*, an event forwarded into an event store you already erased finds no key there, copies the survivor back, and makes already-shredded PII readable again.

> [!CAUTION]
> If you forward a subject's events between event stores, erase that subject in **every** event store and namespace, and do it once forwarding for that subject has stopped. See [Erasing a subject](../compliance/erasing-a-subject.md) for the complete erasure.

## Observing the copy

Chronicle logs each copy at information level from the forwarding subscriber, naming the encryption key identifier, both event stores and the namespace:

```text
Copied the encryption key for 'person-42' from event store 'Sales' to event store 'Support'
in namespace 'Default' while forwarding events. The subject now holds a key in both event
stores, and erasing it reaches only the one it is asked for
```

This is the only place the propagation surfaces — there is no client API that reports which event stores hold a key for a subject.

## Practical guidance

- Always set an explicit subject for events that carry compliance-protected data.
- Use stable subject values per real-world identity (for example, a person identifier).
- Treat key propagation as a runtime safeguard, not a replacement for your compliance design.
- Budget for the erasure fan-out from the start: the number of event stores a subject's key can reach is the number of stores your subscriptions forward them into.

## See also

- [Outbox and Inbox](outbox-inbox)
- [Implicit Event Store Subscriptions](implicit-subscriptions)
- [Explicit Event Store Subscriptions](explicit-subscriptions)
- [Erasing a subject](../compliance/erasing-a-subject.md)
- [Compliance](../compliance/index.md)
