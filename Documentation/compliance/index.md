---
uid: Chronicle.Compliance
---

# Compliance

Modern applications collect and store personal data. Regulations such as the General Data Protection Regulation (GDPR) require that this data be protected, identifiable, and erasable on demand. Chronicle provides first-class support for compliance requirements directly in the event sourcing model, so that protection is applied consistently and automatically rather than scattered across application code.

## Why compliance belongs in the event store

Event-sourced systems have a structural challenge: the event log is append-only and immutable. Once an event is written, it cannot be changed. This property is what makes event sourcing reliable for audit trails and replays — but it conflicts directly with regulations that grant individuals the right to have their personal data erased.

Chronicle solves this by separating the *structure* of the event log (which remains immutable) from the *content* of values that must be protectable. Two mechanisms work together:

- **Encryption at rest** — Values marked as personally identifiable information (PII) are encrypted when written to the event log. The encryption key is tied to the identity of the person the data belongs to. Revoking the key — which happens during erasure — makes the data permanently unreadable without altering the event log itself.
- **Event redaction** — For cases where the entire event payload must be removed (for example, to comply with a specific erasure request), Chronicle can replace the event's content with a redaction marker that preserves the sequence number and audit metadata without retaining the personal data.

## How encryption works

When an event is appended to the event log, Chronicle inspects the event's schema for properties marked as PII. For each such property, the kernel:

1. Looks up the encryption key for the event source identifier associated with the event. If no key exists yet, a new one is generated and stored.
2. Encrypts the property value using that key.
3. Stores the ciphertext in place of the plaintext value.

When a projection or observer reads the event, Chronicle performs the reverse: it retrieves the key, decrypts each PII property, and delivers the plaintext value to the consumer. If the key has been deleted (because an erasure was requested), decryption fails gracefully and the value is returned as an empty string — the data is gone, but the event slot and all non-PII fields remain intact.

Encryption keys are managed by the `IPIIManager` grain in the Chronicle kernel. Keys are stored and retrieved by event source identifier, so every individual whose data lives in the event store has their own key. Deleting a key is the Chronicle equivalent of GDPR erasure.

## Marking data as PII

Chronicle supports two complementary approaches for marking data as personally identifiable:

| Approach | When to use |
|---|---|
| `[PII]` on an event property | The property is a primitive type or an untyped value that holds personal data in a specific event. |
| `[PII]` on a `ConceptAs<T>` type | The concept itself is inherently personal — any use of this type anywhere in any event is PII by definition. |

The `ConceptAs<T>` approach is the preferred one. It is declared once and applies everywhere the type is used, so there is no risk of forgetting to mark a property in a future event. See [Applying PII to ConceptAs types](pii-with-concepts.md) for details.

For a full description of the `[PII]` attribute and its rules, see [PII Attribute](pii.md).

## Event source identifiers cannot be encrypted

Event source identifiers are the keys used to look up encryption keys, group related events, and drive projections. Encrypting them would make it impossible to retrieve the encryption key in order to decrypt the very event that contains the identifier. Chronicle explicitly disallows applying `[PII]` to any type that inherits from `EventSourceId` or `EventSourceId<T>`.

If the identifier itself is sensitive, use a non-sensitive surrogate key as the event source identifier (for example, a randomly generated `Guid`) and store the sensitive value in a regular event property marked with `[PII]`.

## Related topics

| Topic | Description |
|---|---|
| [PII Attribute](pii.md) | The `[PII]` attribute — rules, usage, and constraints |
| [Applying PII to ConceptAs types](pii-with-concepts.md) | How to mark domain value types as PII once and apply everywhere |
| [Working with compliance from the client](client.md) | How to annotate events and ConceptAs types in your .NET client code |
| [Read models and PII](read-models.md) | How PII encryption affects projections, reducers, and read model queries |
| [Event Redaction](../events/redaction.md) | Removing event content for GDPR right-to-erasure requests |
