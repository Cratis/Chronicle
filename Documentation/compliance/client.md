---
uid: Chronicle.Compliance.Client
---

# Working with compliance from the client

Chronicle's PII encryption is applied transparently by the kernel. From a client perspective, your responsibility is to annotate your domain model correctly so that the kernel knows which values to encrypt. There are two places to put this annotation, and you will typically use both in the same codebase.

## Option 1 — Annotate your event types

Apply `[PII]` directly to individual properties on your event records. This is the simplest starting point and works well when a value is PII only in the context of one specific event.

```csharp
using Cratis.Chronicle.Compliance.GDPR;

[EventType]
public record EmployeeRegistered(
    [PII] string FirstName,
    [PII] string LastName,
    string Department,
    DateTimeOffset StartDate);
```

`FirstName` and `LastName` are encrypted when the event is written. `Department` and `StartDate` are stored as plaintext.

### When to use this approach

Use property-level annotation when:

- The property type is a primitive (`string`, `int`) and you do not need a dedicated concept type.
- The value is PII in this event but is conceptually not a PII concept elsewhere.
- You are incrementally adding compliance to an existing model and want to make targeted changes.

### Limitation

The main drawback of property-level annotation is that you must remember to add `[PII]` every time a new event carries the same kind of value. If a future event includes an employee's name without the attribute, it will be written as plaintext.

## Option 2 — Annotate your ConceptAs types

Apply `[PII]` to a `ConceptAs<T>` concept type. Chronicle then encrypts every event property that uses that type, automatically, across all events.

```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record PersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PersonName NotSet = new(string.Empty);
    public static implicit operator string(PersonName name) => name.Value;
    public static implicit operator PersonName(string value) => new(value);
}
```

Any event that uses `PersonName` is automatically encrypted — no further annotation is needed:

```csharp
[EventType]
public record EmployeeRegistered(PersonName Name, string Department);

[EventType]
public record EmployeeNameChanged(PersonName NewName);  // also encrypted
```

> [!TIP]
> For the full `ConceptAs<T>` declaration pattern and placement rules, see [Applying PII to ConceptAs types](pii-with-concepts.md) and <xref:Fundamentals.Concepts>.

### When to use this approach

Use concept-level annotation when:

- The value represents a domain concept that is inherently personal (a person's name, email address, phone number, national ID).
- The same kind of value appears in multiple events and you want consistent protection without repeating the attribute.
- You are building a new domain model and want compliance baked in from the start.

This is the **recommended approach** for any value that is personal by nature.

## Combining both approaches

The two approaches complement each other. Use concept types for inherently personal domain values, and use property-level annotation for one-off cases where creating a concept type is not warranted.

```csharp
[PII]
public record PersonName(string Value) : ConceptAs<string>(Value) { ... }

[PII]
public record EmailAddress(string Value) : ConceptAs<string>(Value) { ... }

[EventType]
public record CustomerRegistered(
    PersonName Name,            // encrypted via concept type
    EmailAddress Email,         // encrypted via concept type
    [PII] string PhoneNumber,  // encrypted via property annotation
    string Country);            // plaintext
```

## EventSourceId cannot be marked PII

Do not apply `[PII]` to types that inherit from `EventSourceId` or `EventSourceId<T>`. Chronicle throws `PIINotSupportedOnEventSourceId` because event source identifiers are required for key lookup and cannot be encrypted.

```csharp
// ❌ This will throw PIINotSupportedOnEventSourceId at startup
[PII]
public record CustomerId(Guid Value) : EventSourceId<Guid>(Value);
```

If the identifier itself is sensitive, use a non-sensitive surrogate key as the event source identifier (a randomly generated `Guid` works well) and store the sensitive value in a `[PII]`-marked event property.

## Registering compliance services

Compliance support is registered with a single extension method in your ASP.NET Core setup:

```csharp
builder.Services.AddCompliance();
```

This registers `PIIMetadataProvider` and all supporting infrastructure needed for the kernel to discover and encrypt PII properties.
