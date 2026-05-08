# Compliance and PII

When subscriptions forward events from a source event store outbox to a target event store inbox, Chronicle preserves compliance identity so encrypted data remains decryptable downstream.

## What is preserved during forwarding

For each forwarded event, Chronicle preserves:

- **Subject** from `EventContext.Subject`
- **Encryption key identity** derived from that subject
- **Encryption key availability** in the target event store namespace when missing

This keeps encryption and decryption behavior stable across stores for the same person or entity.

## Why this matters

PII fields are encrypted using a key tied to compliance identity. If forwarding changed identity or failed to make the key available in the target store, encrypted payloads could be unreadable for downstream processing.

By preserving subject identity and propagating missing keys, Chronicle keeps the compliance boundary consistent while events move between stores.

## Forwarding behavior

When an outbox event is forwarded to an inbox:

1. Chronicle uses the event subject as the compliance identity.
2. Chronicle checks whether the target store namespace already has a key for that identity.
3. If the target key is missing and the source key exists, Chronicle copies the key to the target store namespace.
4. Chronicle appends the event to inbox with the original subject preserved.

## Practical guidance

- Always set an explicit subject for events that carry compliance-protected data.
- Use stable subject values per real-world identity (for example, a person identifier).
- Treat key propagation as a runtime safeguard, not a replacement for your compliance design.

## See also

- [Outbox and Inbox](outbox-inbox.md)
- [Implicit Event Store Subscriptions](implicit-subscriptions.md)
- [Explicit Event Store Subscriptions](explicit-subscriptions.md)
- [Compliance](../compliance/index.md)
