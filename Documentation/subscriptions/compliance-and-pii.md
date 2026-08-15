# Compliance and PII

When subscriptions forward events from a source event store outbox to a target event store inbox, Chronicle preserves the compliance identity of every event — and copies the subject's encryption key along with it, unless that subject has been erased in the target. Both halves matter for right-to-erasure, and this page covers what each of them does.

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
3. If the target key is missing, Chronicle checks whether the subject was **erased** there. If it was, no key is copied and the refusal is logged.
4. Otherwise, if the source key exists, Chronicle copies it to the target store namespace and logs that it did so.
5. Chronicle appends the event to the inbox with the original subject preserved.

Step 4 is what makes the two event stores share one key pair per subject. It is not what makes the forwarded payload readable: the forwarding subscriber receives events already decrypted, and the append in step 5 re-encrypts every `[PII]` value under whatever key the target store holds for that subject. Forwarding works either way; the copy is what keeps the *same* key on both sides.

## Why this matters for erasure

The copy means a subject's key can live in more event stores than you ever appended to. Chronicle handles both consequences of that:

- **An erasure covers every event store in the namespace**, which is exactly how far the copy can reach. Erasing through any one event store reaches them all — see [Erasing a subject](../compliance/erasing-a-subject.md).
- **The copy cannot resurrect an erased key.** Step 3 above is the reason. Before the erasure was recorded in the target, "the target has no key" was indistinguishable from "the target never had one" — which is precisely the state an erasure creates, so a forwarded event copied the survivor back in and made already-shredded personal data readable again.

> [!IMPORTANT]
> A forwarded event that carries `[PII]` for an erased subject cannot be appended in the target event store: there is no key to protect it with, and Chronicle will not mint one for an erased subject. The observer partition for that event source enters a failed state and the rest of the subscription keeps flowing. Either stop producing that subject's personal data, or authorize a new key with `AllowNewEncryptionKeyFor`.

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

The subject is deliberately left out of both. It is the identity the personal data belongs to, and a log entry naming it is unencrypted personal data that outlives the erasure the key deletion performs.

## Practical guidance

- Always set an explicit subject for events that carry compliance-protected data.
- Use stable subject values per real-world identity (for example, a person identifier).
- Treat key propagation as a runtime safeguard, not a replacement for your compliance design.
- Erase once per namespace, not once per event store — and quiesce or expect failed partitions for any subject still being forwarded.

## See also

- [Outbox and Inbox](outbox-inbox)
- [Implicit Event Store Subscriptions](implicit-subscriptions)
- [Explicit Event Store Subscriptions](explicit-subscriptions)
- [Erasing a subject](../compliance/erasing-a-subject.md)
- [The encryption key lifecycle](../compliance/key-lifecycle.md)
- [Compliance](../compliance/index.md)
