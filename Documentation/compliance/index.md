---
title: Compliance
description: Protect personally identifiable information in an immutable event log with encryption, erasure, and redaction.
---

Cratis Chronicle protects personally identifiable information (PII) in events with encryption tied to the
person the data belongs to. Use it when you need to retain event history while making that person's
protected values unreadable after erasure. You declare which values are PII; Chronicle encrypts
them as they enter the event log.

These mechanisms support work such as General Data Protection Regulation (GDPR) erasure requests;
they do not determine whether an application meets a regulation. [Regulatory responsibilities](regulatory-responsibilities.md)
separates Chronicle's mechanisms from the configuration and policies you own, including HIPAA,
SEC/PCAOB, PCI DSS, and cross-border transfers.

## Why compliance belongs in the event store

Replaying an event log depends on retaining the facts that led to the current state. Personal data
has a different requirement: you may need to erase it without losing the rest of that history.
Chronicle separates the event's place in the log from the protected values it carries.

:::note[Not every secret is PII]
A value can need encryption at rest without being personal data — an API key, a webhook signing secret. Marking it `[PII]` enrolls it in GDPR right-to-erasure, which is the wrong lifecycle for a secret with no data subject. See [Security](/chronicle/security/) for `[Encrypted]`, the deliberately separate mechanism for that case.
:::

Encryption protects individual PII values at rest. Their key belongs to the person identified as
the event's subject; erasing that key makes the protected values unreadable without removing the
event from the log.

[Event redaction](../events/redaction) is the other option when the entire payload must be removed.
It replaces the content with a redaction marker while preserving the sequence number and audit
metadata. Choose the mechanism according to what must be erased, rather than treating erasure as
deleting the event's place in history.

## How encryption works

When an event is appended, the Chronicle server (the kernel) inspects the event's schema for PII
properties. For each one, it:

1. Looks up the encryption key for the event's [subject](../concepts/subject.mdx) — the event source identifier, unless the append names a different subject. If no key exists yet, a new one is generated and stored.
2. Encrypts the property value using that key.
3. Stores the ciphertext in place of the plaintext value.

When a projection or observer reads the event, Chronicle retrieves the key and decrypts the PII
properties for that consumer. After erasure, those properties return an empty string, `0`, `false`,
or `null`, depending on their type, as described in [Erasing a subject](erasing-a-subject). The
event's position and non-PII fields remain intact, so consumers must handle those erased values.

The kernel's `IPIIManager` manages keys by subject, giving each person a separate key.
Erasure removes that key; it does not remove copies of plaintext that an application has already
sent elsewhere.

A key belongs to one event store and one namespace, and an event store subscription copies a subject's key into the event store it forwards to. An erasure therefore covers every event store in the namespace it is issued in, and records itself there so that nothing provisions or copies a key for that subject afterwards. [Erasing a subject](erasing-a-subject) walks through the call and what it reaches; [The encryption key lifecycle](key-lifecycle) explains the mechanism and its limits.

## Marking data as PII

Chronicle supports two complementary approaches for marking data as personally identifiable:

| Approach | When to use |
|---|---|
| `[PII]` on an event property | The property is a primitive type or an untyped value that holds personal data in a specific event. |
| `[PII]` on a `ConceptAs<T>` type | The concept itself is inherently personal — any use of this type anywhere in any event is PII by definition. |

Prefer the concept-level marker when the value itself is personal. In the
[.NET concept example](pii-with-concepts), `PiiConceptsPersonName` wraps a string with
`ConceptAs<string>` and carries `[PII]`. An event property using that type inherits its
classification, rather than needing a repeated marker on every property. Use a property-level
marker when the classification belongs to that use of the value instead.

Adding a marker later does not encrypt earlier stored values. Before changing an existing event
contract, follow [Adding PII to an existing event type](pii#adding-pii-to-an-existing-event-type).
[Marking data as PII](pii) covers the marker's rules and supported declaration paths.

## Event source identifiers cannot be encrypted

Event source identifiers are the keys used to look up encryption keys, group related events, and drive projections. Encrypting them would make it impossible to retrieve the encryption key in order to decrypt the very event that contains the identifier. Chronicle explicitly disallows applying `[PII]` to any type that inherits from `EventSourceId` or `EventSourceId<T>`.

If the identifier itself is sensitive, use a non-sensitive surrogate key as the event source identifier (for example, a randomly generated `Guid`) and store the sensitive value in a regular event property marked with `[PII]`.

## Related topics

| Topic | Description |
|---|---|
| [Marking data as PII](pii) | The `[PII]` marker — rules, usage, and constraints |
| [Declaring PII on a concept](pii-with-concepts) | How to mark domain value types as PII once and apply everywhere |
| [Working with compliance from the client](client) | How to annotate events and ConceptAs types in your .NET client code |
| [Read models and PII](read-models) | How PII encryption affects projections, reducers, and read model queries |
| [Erasing a subject](erasing-a-subject) | What one erasure reaches, and how to erase across every event store |
| [Regulatory responsibilities](regulatory-responsibilities.md) | What Chronicle provides, what you configure, and which regulatory obligations stay with your application |
| [Compliance in Arc applications](arc.md) | Using read models and queries with compliance in Arc |
| [Event Redaction](../events/redaction) | Removing event content for GDPR right-to-erasure requests |
