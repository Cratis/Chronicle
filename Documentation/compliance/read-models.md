---
uid: Chronicle.Compliance.ReadModels
---

# Read models and PII

Chronicle stores PII fields encrypted at rest in both the event log and managed read models (projections and reducers). The encryption and decryption is handled automatically by the kernel — your application code works with plaintext values at all times.

## Observer decryption

All observers — reactors, webhooks, reducers, and projections — receive decrypted events from a single, central decryption point in `Observer.Handle()`. This means encryption is applied consistently regardless of the observer type, and no observer implementation needs to handle decryption itself.

The compliance identifier used for key lookup follows this rule: if an explicit `Subject` was set on the event at append time, that value is used. Otherwise, the `EventSourceId` is used as the fallback identifier. This mirrors the encryption key that was used when the event was originally written to the event log.

## Projections — automatic PII lineage

Projection-backed read models benefit from automatic PII lineage. The kernel knows which read model properties are mapped from PII event properties and encrypts them transparently before writing to storage. No `[PII]` attribute is needed on the read model type.

```csharp
[EventType]
public record EmployeeRegistered(PersonName Name, string Department);

[ReadModel]
[FromEvent<EmployeeRegistered>]
public record Employee(
    [Key] EmployeeId Id,
    string Name,        // mapped from PersonName — stored encrypted at rest
    string Department)
{
    public static ISubject<Employee?> ById(EmployeeId id) =>
        Query.ForEventSource<Employee>(id);
}
```

The `Name` property is stored encrypted in MongoDB because `PersonName` is a PII-marked type. When a query returns `Employee` records, the kernel decrypts the values before they reach the caller.

## Reducers — explicit `[PII]` required

Reducers use arbitrary C# logic to compute state. The kernel cannot infer which properties derive from PII fields because the transformation is opaque. You must annotate PII properties on the read model record with `[PII]`.

```csharp
[EventType]
public record PatientAdmitted(PersonName Name, DateTimeOffset AdmittedAt);

public record PatientSummary(Guid PatientId, [PII] string Name, DateTimeOffset LastAdmittedAt);

public class PatientSummaryReducer : IReducerFor<PatientSummary>
{
    public Task<PatientSummary> On(PatientAdmitted @event, PatientSummary? current, EventContext context)
    {
        return Task.FromResult(new PatientSummary(
            Guid.Parse(context.EventSourceId.Value),
            @event.Name,
            @event.AdmittedAt));
    }
}
```

The `[PII]` attribute on `Name` tells the kernel to encrypt that property before storage and decrypt it on retrieval.

## The `_subject` field

Every managed read model document written by Chronicle contains a reserved `_subject` field. This field stores the compliance identifier (the `Subject` or `EventSourceId`) that was used as the encryption key reference for that document. Chronicle uses `_subject` to look up the correct key when decrypting on retrieval.

> **Do not** declare a property named `_subject` in your read model records. Chronicle reserves this name for internal use.

## GDPR erasure

Deleting an encryption key is the Chronicle mechanism for GDPR erasure:

1. Delete the key for the subject (the data subject's identifier) via the Compliance API.
2. Trigger a re-projection or re-reduction of the affected read models.

After key deletion, decryption of PII properties for that subject fails gracefully — Chronicle writes empty values for erased PII fields. Existing read model documents that were written before erasure continue to contain encrypted ciphertext until they are re-projected.

> [!IMPORTANT]
> Re-projection is required for a complete, verifiable erasure. Until re-projection runs, the ciphertext remains in the read model store. Chronicle cannot decrypt it, but the ciphertext is still present.

For full erasure of event content, combine key deletion with [event redaction](../events/redaction.md).

## Querying

Read model queries are transparent to PII encryption. No changes to query code are needed:

```csharp
[ReadModel]
public record Employee(EmployeeId Id, string Name, string Department)
{
    public static ISubject<Employee?> ById(EmployeeId id) =>
        Query.ForEventSource<Employee>(id);
}
```

Chronicle decrypts PII fields automatically before returning results to the caller. When the encryption key has been deleted for a subject, `Name` returns an empty string — the caller receives an `Employee` record with an empty name, never an exception or partial result.
