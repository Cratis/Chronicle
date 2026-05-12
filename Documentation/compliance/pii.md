---
uid: Chronicle.Compliance.PII
---

# PII Attribute

The `[PII]` attribute marks data as personally identifiable information (PII) under GDPR. When Chronicle sees this attribute on an event property or a `ConceptAs<T>` type, it encrypts the value automatically when the event is written to the event log and decrypts it transparently on read.

```csharp
using Cratis.Chronicle.Compliance.GDPR;
```

## Attribute definition

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PIIAttribute(string details = "") : Attribute
```

The optional `details` parameter lets you record *why* the value is classified as PII — for example, the legal basis under which it is collected or the retention period. This information is stored in the event schema and can be used by compliance reporting tools.

```csharp
[PII("Collected under GDPR Art. 6(1)(b) — necessary for contract performance")]
public record PersonName(string Value) : ConceptAs<string>(Value);
```

## Where the attribute is valid

| Target | Supported | Notes |
|---|---|---|
| `ConceptAs<T>` class | Yes | Preferred approach — marks the concept type itself as PII |
| Event property | Yes | Marks a single property of an event as PII |
| `EventSourceId` or `EventSourceId<T>` | **No** | Throws `PIINotSupportedOnEventSourceId` at runtime |
| Any other class type | **No** | Throws `PIIAppliedToNonConceptAsType` at runtime |

## Applying to an event property

You can mark a single property on an event record as PII. This is useful when the property type is a primitive and you cannot or do not want to introduce a dedicated concept type.

```csharp
[EventType]
public record EmployeeRegistered(
    [PII] string FirstName,
    [PII] string LastName,
    string Department);
```

When this event is written, `FirstName` and `LastName` are encrypted. `Department` is stored as plaintext.

## Applying to a ConceptAs type

The preferred approach is to mark the `ConceptAs<T>` type itself as PII. Every property across every event that uses this type is then automatically encrypted — you declare the rule once and it applies everywhere.

```csharp
[PII]
public record PersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PersonName NotSet = new(string.Empty);
    public static implicit operator string(PersonName name) => name.Value;
    public static implicit operator PersonName(string value) => new(value);
}
```

> [!TIP]
> Use <xref:Fundamentals.Concepts> as a reference for the full `ConceptAs<T>` pattern. For guidance on declaring compliance on concept types, see [Applying PII to ConceptAs types](pii-with-concepts.md).

## Constraints

### EventSourceId is not supported

Applying `[PII]` to a type that inherits from `EventSourceId` or `EventSourceId<T>` throws `PIINotSupportedOnEventSourceId` at runtime:

```csharp
// ❌ This will throw PIINotSupportedOnEventSourceId
[PII]
public record EmployeeId(Guid Value) : EventSourceId<Guid>(Value);
```

Event source identifiers are used to look up encryption keys and group events. Encrypting them would make key lookup impossible. If the identifier itself is sensitive, use a non-sensitive surrogate (such as a random `Guid`) as the event source identifier and store the sensitive value in a `[PII]`-marked event property.

### Non-ConceptAs class types are not supported

Applying `[PII]` to a class that does not inherit from `ConceptAs<T>` throws `PIIAppliedToNonConceptAsType` at runtime:

```csharp
// ❌ This will throw PIIAppliedToNonConceptAsType
[PII]
public class SomeArbitraryClass { }
```

The `[PII]` attribute at the class level is reserved for `ConceptAs<T>` types. Use a property-level `[PII]` annotation instead if you need to mark individual fields on a class.

## Details parameter

The `details` parameter is a free-text description stored in the event schema. It is never used for encryption — it exists solely to record *why* a value is classified as PII for compliance documentation and auditing purposes.

```csharp
[PII("Full legal name — required for contract identification")]
public record LegalName(string Value) : ConceptAs<string>(Value);
```
