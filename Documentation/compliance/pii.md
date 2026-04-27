---
applyTo: "**"
---

# PII

**Personally Identifiable Information** — names, addresses, national identification numbers,
contact details, and anything else that can identify a real person — is the most common
compliance concern. GDPR, CCPA, and similar regimes all require that PII can be *deleted*
on request, even when the system is fundamentally append-only.

Chronicle solves this by encrypting PII properties with a **per-subject encryption key**. When
the key is deleted, every PII property for that subject becomes unrecoverable — the events
themselves remain intact, but the sensitive fields are permanently inaccessible.

## Marking properties as PII

Annotate the specific properties that contain PII. There are two ways:

### The `[PII]` attribute

```csharp
[EventType]
public record CustomerRegistered(
    CustomerId CustomerId,
    string CompanyName,
    [property: PII] string ContactEmail,
    [property: PII] string ContactPhone);
```

Only properties marked with `[PII]` are encrypted. Plain properties are stored as written.

### The `PIIConceptAs<T>` base record

When the PII nature of a value is intrinsic to its meaning (a social security number is always
PII no matter where it appears), define the value type itself as PII:

```csharp
public record SocialSecurityNumber(string Value) : PIIConceptAs<string>(Value);

[EventType]
public record TaxpayerRegistered(TaxpayerId Id, SocialSecurityNumber Ssn);
```

Any property typed as `SocialSecurityNumber` is automatically encrypted without needing an
attribute at every use site.

## Subjects and encryption keys

Encryption keys are stored under a [`Subject`](./../concepts/subject.md). By default, the
subject is the `EventSourceId` of the append. When you append an event that carries PII for
the first time for a subject, Chronicle:

1. Generates a unique RSA key pair for that subject,
2. Stores it in the encryption key store (MongoDB or Entity Framework-backed),
3. Encrypts each `[PII]` property with the public key.

Subsequent appends reuse the same key. Reads decrypt transparently with the stored private
key.

## Explicit subjects

When the subject of an event is different from its event source, pass `Subject` explicitly:

```csharp
await eventStore.EventLog.Append(
    eventSourceId: orderId,
    @event: new ShippingAddressChanged(street, city),
    subject: customerId);
```

This keys the order's PII under the *customer*, not under the order. When that customer
invokes their right to erasure, a single key deletion scrubs every order, invoice, and
interaction record at once.

## Implicit subject with `[Subject]`

To avoid threading the subject through every call site, mark the responsible property with
`[Subject]`. Chronicle derives the subject automatically at append time:

```csharp
[EventType]
public record ShippingAddressChanged(
    OrderId Order,
    [property: Subject] CustomerId Customer,
    [property: PII] string Street,
    [property: PII] string City);

// Subject is read from the Customer property automatically.
await eventLog.Append(orderId, new ShippingAddressChanged(orderId, customerId, "123 Main St", "Springfield"));
```

An explicit `subject` argument always takes precedence over a `[Subject]` property.

## Honoring deletion requests

Deleting a subject's encryption key is the compliance operation. Use the `IPIIManager` grain:

```csharp
await piiManager.DeleteEncryptionKeyFor(customerId);
```

After the delete, the events still exist — their non-PII fields still project correctly — but
any attempt to read a PII property fails because the key no longer exists. This preserves the
integrity of the event log while satisfying the "right to be forgotten".
