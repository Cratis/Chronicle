---
uid: Chronicle.Compliance.PIIWithConcepts
---

# Applying PII to ConceptAs types

The most robust way to declare that a value is personally identifiable is to mark the `ConceptAs<T>` type itself with `[PII]`. Doing so means the declaration lives exactly once — on the type — and every event property that uses that type is automatically encrypted, regardless of where or how many times it appears across your event model.

> [!TIP]
> For background on the `ConceptAs<T>` pattern, see <xref:Fundamentals.Concepts>.

## Why concept-level declaration is preferred

When you mark a property directly with `[PII]`, you must remember to repeat the attribute on every event or read model that carries that value.
If a new event is added months later and the developer forgets the annotation, a plaintext value is written to the event log with no warning.

A concept type solves this by making the protection part of the type's identity. You cannot use `PersonName` without the encryption — the two are inseparable.

```csharp
// ❌ Property-level: requires repetition across every event
[EventType]
public record EmployeeRegistered([PII] string Name, string Department);

[EventType]
public record EmployeeNameChanged([PII] string NewName);  // must remember [PII] again

// ✅ Concept-level: declare once, apply everywhere automatically
[PII]
public record PersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PersonName NotSet = new(string.Empty);
    public static implicit operator string(PersonName name) => name.Value;
    public static implicit operator PersonName(string value) => new(value);
}

[EventType]
public record EmployeeRegistered(PersonName Name, string Department);  // Name is encrypted

[EventType]
public record EmployeeNameChanged(PersonName NewName);  // also encrypted, no extra annotation needed
```

## Defining a PII concept type

Follow the standard `ConceptAs<T>` pattern and add `[PII]` to the record declaration:

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

For a `Guid`-backed concept:

```csharp
[PII("National ID number — sensitive personal identifier")]
public record NationalIdNumber(string Value) : ConceptAs<string>(Value)
{
    public static readonly NationalIdNumber NotSet = new(string.Empty);

    public static implicit operator string(NationalIdNumber id) => id.Value;
    public static implicit operator NationalIdNumber(string value) => new(value);
}
```

## Placement

Concept types belong in the feature folder they are primarily associated with, or at the feature root if they are shared across slices. Do not create a separate `Concepts/` folder — keep concepts co-located with the code that uses them.

```
Features/
├── Employees/
│   ├── PersonName.cs          ← shared across Registration and Updates slices
│   ├── Registration/
│   │   ├── Registration.cs    ← uses PersonName
│   └── Updates/
│       ├── Updates.cs         ← also uses PersonName — encryption is automatic
```

## Combining with details

Use the optional `details` parameter on `[PII]` to document the legal basis or purpose of the PII classification. This is stored in the event schema and can be surfaced by compliance tooling.

```csharp
[PII("Collected under GDPR Art. 6(1)(b) — necessary for contract performance. Retention: contract duration + 7 years.")]
public record LegalName(string Value) : ConceptAs<string>(Value)
{
    public static readonly LegalName NotSet = new(string.Empty);

    public static implicit operator string(LegalName name) => name.Value;
    public static implicit operator LegalName(string value) => new(value);
}
```

## What cannot be marked PII

### EventSourceId types

Any type inheriting from `EventSourceId` or `EventSourceId<T>` cannot be marked with `[PII]`. Chronicle throws `PIINotSupportedOnEventSourceId` if you attempt this.

```csharp
// ❌ Throws PIINotSupportedOnEventSourceId
[PII]
public record EmployeeId(Guid Value) : EventSourceId<Guid>(Value);
```

Use a non-sensitive surrogate key as the event source identifier and store sensitive identity values in a separate event property:

```csharp
// ✅ Surrogate key as event source identifier
public record EmployeeId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static EmployeeId New() => new(Guid.NewGuid());
}

// ✅ Sensitive value stored in a PII-marked concept type
[PII("Employee national ID — sensitive identifier")]
public record NationalIdNumber(string Value) : ConceptAs<string>(Value);

[EventType]
public record EmployeeRegistered(NationalIdNumber NationalId, PersonName Name);
```
