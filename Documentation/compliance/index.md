---
applyTo: "**"
---

# Compliance

Chronicle has built-in support for compliance concerns that event-sourced systems repeatedly
have to solve — principally around personal data. Today the primary focus is GDPR-style PII
handling, but the infrastructure is designed to extend to other regulatory concerns as they
arise.

## Pages

- [PII](./pii.md) — marking PII properties, per-subject encryption keys, honoring deletion requests.
- [Working with Compliance from the Client](./client.md) — practical how-to guide covering event annotation, subject derivation, and the right-to-erasure workflow.

## Designing for compliance

Two conventions make compliance work cleanly with the rest of the framework:

1. **Mark facts at the type level.** Decorate properties with `[PII]` (or derive a value type
   from `PIIConceptAs<T>`) on the event records and read models where the data lives. Compliance
   metadata flows automatically through the JSON schema to the kernel.
2. **Identify the subject.** A [`Subject`](./../concepts/subject.md) is the target the data is
   about — typically a person. Chronicle keys encryption material per subject, so a single
   `DeleteEncryptionKey` call for a subject invalidates every PII property ever written for
   them across every event type.

Subjects default to the `EventSourceId` when not specified, which covers the common case
where the aggregate and the subject coincide.
