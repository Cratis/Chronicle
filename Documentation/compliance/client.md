---
applyTo: "**"
---

# Working with Compliance from the Client

This page is a practical how-to guide that walks through the full compliance workflow from the .NET client perspective — from annotating event types to honoring deletion requests.

## 1 — Annotate your event types

### Marking individual properties with `[PII]`

Add `[PII]` to any property in an event record that holds personally identifiable information:

```csharp
using Cratis.Chronicle.Compliance.GDPR;

[EventType]
public record CustomerRegistered(
    CustomerId Id,
    string CompanyName,
    [PII] string ContactEmail,
    [PII] string ContactPhone);
```

Only the annotated properties are encrypted. Plain properties such as `CompanyName` are stored in clear text.

### Using `PIIConceptAs<T>` for intrinsically sensitive values

When a value is always PII regardless of context — a national identification number, a health record reference — define its type to carry that fact:

```csharp
public record SocialSecurityNumber(string Value) : PIIConceptAs<string>(Value);

[EventType]
public record TaxpayerRegistered(TaxpayerId Id, SocialSecurityNumber Ssn);
```

Any property typed as `SocialSecurityNumber` is automatically encrypted without needing a `[PII]` attribute at every use site.

## 2 — Identify the subject

A **subject** is the person the event data is *about* — the identity that encryption keys are stored under. By default Chronicle uses the `EventSourceId` as the subject, which is correct when the aggregate and the person are the same entity.

### Implicit subject via `[Subject]`

When the subject differs from the event source, mark the relevant property with `[Subject]`. Chronicle will pick it up automatically at append time:

```csharp
using Cratis.Chronicle.Compliance.GDPR;

[EventType]
public record ShippingAddressChanged(
    OrderId Order,
    [Subject] CustomerId Customer,
    [PII] string Street,
    [PII] string City);
```

Now you can append without providing an explicit subject:

```csharp
await eventLog.Append(orderId, new ShippingAddressChanged(orderId, customerId, "123 Main St", "Springfield"));
```

Chronicle reads the `Customer` property and keys the PII encryption under `customerId`. When that customer later requests erasure, one key deletion covers every order, invoice, and interaction that carries their address.

### Explicit subject at the call site

If you prefer — or if the subject cannot be inferred from the event itself — pass it directly:

```csharp
await eventLog.Append(
    eventSourceId: orderId,
    @event: new ShippingAddressChanged(orderId, customerId, "123 Main St", "Springfield"),
    subject: customerId);
```

An explicit subject at the call site always takes precedence over a `[Subject]` property. The implicit `[Subject]` resolution only runs when `subject` is `null`.

### Implicit subject — same entity as the event source

For aggregates where the subject _is_ the event source you need nothing extra. Omit both `[Subject]` and the `subject` parameter and Chronicle uses the `EventSourceId`:

```csharp
await eventLog.Append(authorId, new AuthorRegistered("John Doe"));
// PII is keyed under authorId
```

## 3 — Honor deletion requests (right to erasure)

When a user invokes their right to erasure, delete the encryption key for their subject. All PII properties ever written for that subject become permanently unrecoverable in one call.

Inject `IPIIManager` into any service, handler, or controller:

```csharp
using Cratis.Chronicle.Compliance.GDPR;

public class CustomerService(IPIIManager piiManager)
{
    public async Task ProcessErasureRequest(CustomerId customerId)
    {
        await piiManager.DeleteEncryptionKeyFor(customerId.ToString());
    }
}
```

After the key is deleted:

- The events themselves remain in the store — their non-PII fields still project correctly.
- Reading any PII property for that subject fails because the key is gone.
- The right-to-erasure requirement is satisfied.

## 4 — Decrypting read models with `Release()`

When you read a projection or reducer that includes PII fields, those fields are stored in their encrypted form. Call `Release()` on `IReadModels` to decrypt them before presenting the data to a user.

### Identifying the subject on a read model

Chronicle applies the same subject resolution rules to read models as to events:

1. A property decorated with `[Subject]` is used as the subject.
2. A constructor parameter decorated with `[Subject]` (record shorthand) is used as the subject.
3. A property named `Id` (case-insensitive) is used as a final fallback.

```csharp
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string CompanyName,
    [PII] string ContactEmail);
```

### Decrypting a single instance

```csharp
public class CustomerProfileController(IReadModels readModels)
{
    public async Task<IActionResult> GetProfile(CustomerId customerId)
    {
        var profile = await readModels.GetInstanceById<CustomerProfile>(customerId.ToString());
        var decrypted = await readModels.Release(profile);
        return Ok(decrypted);
    }
}
```

Chronicle derives the subject from `profile.CustomerId`, calls the kernel to decrypt, and returns the plaintext read model.

### Decrypting a collection

```csharp
var profiles = await readModels.GetInstances<CustomerProfile>();
var decrypted = await readModels.Release(profiles);
```

Each instance is decrypted individually using the subject derived from that instance.

### Decrypting with an explicit subject

When the subject cannot be derived from the read model itself, pass it directly:

```csharp
var profile = await readModels.GetInstanceById<CustomerProfile>(customerId.ToString());
var decrypted = await readModels.Release((Subject)customerId.ToString(), profile);
```

### Behavior on failure

`Release()` is designed to be non-breaking:

- If the read model has **no PII-annotated properties**, the original instance is returned immediately without contacting the server.
- If the encryption key **no longer exists** (e.g. after a right-to-erasure request), the original encrypted instance is returned and an error is logged. The call does not throw.

```csharp
// After erasure, this returns the instance with encrypted field values unchanged
// and logs an error — it does not throw.
var profile = await readModels.Release(erasedCustomerProfile);
```

## Putting it all together

The example below combines all the pieces — event definition, subject annotation, PII encryption, erasure, and decryption:

```csharp
// Domain types
public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator CustomerId(Guid value) => new(value);
    public static implicit operator Guid(CustomerId id) => id.Value;
}

public record FullName(string Value) : PIIConceptAs<string>(Value);

// Event type
[EventType]
public record CustomerProfileUpdated(
    [Subject] CustomerId CustomerId,
    FullName Name,
    [PII] string Email);

// Read model
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string Name,
    [PII] string Email);

// Appending — subject derived automatically from [Subject]
await eventLog.Append(customerId, new CustomerProfileUpdated(customerId, "Jane Doe", "jane@example.com"));

// Reading and decrypting
var profile = await readModels.GetInstanceById<CustomerProfile>(customerId.ToString());
var decrypted = await readModels.Release(profile);
// decrypted.Email == "jane@example.com"

// Erasure
await piiManager.DeleteEncryptionKeyFor(customerId.ToString());

// After erasure — returns encrypted form, logs error, no exception
var erasedProfile = await readModels.Release(profile);
```
