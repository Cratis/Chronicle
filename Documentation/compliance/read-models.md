---
uid: Chronicle.Compliance.ReadModels
---

# Read models and PII

Read models are built by projecting events from the event log. When those events contain PII-encrypted values, Chronicle decrypts them transparently before delivering the event data to your projection or reducer. The read model itself stores the decrypted plaintext — the encryption boundary is the event log.

## How decryption flows through projections

When a projection processes an event, the Chronicle kernel:

1. Reads the encrypted ciphertext from the event log.
2. Looks up the encryption key for the event source identifier.
3. Decrypts all PII properties before passing the event to the projection handler.
4. The projection writes the decrypted value into the read model.

This means your projection code and read model records are written exactly as if the data were never encrypted:

```csharp
[EventType]
public record EmployeeRegistered(PersonName Name, string Department);

[ReadModel]
public record Employee(EmployeeId Id, string Name, string Department)
{
    // ...
}

public class EmployeeProjection : IProjectionFor<Employee>
{
    public void Define(IProjectionBuilderFor<Employee> builder) =>
        builder.From<EmployeeRegistered>(e => e
            .Set(m => m.Name).To(ev => ev.Name)   // PersonName is already decrypted
            .Set(m => m.Department).To(ev => ev.Department));
}
```

## What happens when the encryption key is deleted

Deleting an encryption key is the Chronicle mechanism for GDPR erasure. Once the key is gone, decryption of PII properties for that event source will fail. Chronicle handles this gracefully:

- PII properties decrypt to an empty value (empty string for `string`-backed types).
- Non-PII properties are returned as-is.
- The read model update still runs — it just stores empty values for the PII fields.
- Existing read model documents that were written before erasure continue to contain the decrypted value until they are re-projected.

> [!IMPORTANT]
> If a complete and verifiable erasure is required, you must trigger a re-projection of the read model after deleting the key. This overwrites any cached plaintext values in the read model store.

## Re-projecting after key deletion

Chronicle's catch-up projection mechanism lets you replay all events and rebuild the read model from scratch. After deleting the key for a particular event source, force a catch-up for that event source to ensure the read model reflects the erased state.

## Querying PII data

Read model queries return the decrypted plaintext value. There is nothing special to do in query code:

```csharp
[ReadModel]
public record Employee(EmployeeId Id, string Name, string Department)
{
    public static ISubject<Employee?> ById(EmployeeId id) =>
        Query.ForEventSource<Employee>(id);
}
```

The `Name` field contains the decrypted person name when the key exists, or an empty string when the key has been deleted.

## Storing PII in read models

Because read models store decrypted plaintext, they are themselves subject to data protection requirements. Consider:

- Applying appropriate access controls to the read model store (MongoDB collection permissions, for example).
- Treating the read model store as sensitive data in your infrastructure security posture.
- Re-projecting affected read models as part of your GDPR erasure workflow to propagate key deletion.

For full erasure of event content, combine key deletion with [event redaction](../events/redaction.md).

## Model-bound projections and PII

Model-bound projections using `[FromEvent<T>]` attributes behave identically — decryption happens at the event layer before the attribute mapping runs.

```csharp
[ReadModel]
[FromEvent<EmployeeRegistered>]
public record Employee(
    [Key] EmployeeId Id,
    string Name,        // mapped from PersonName — arrives decrypted
    string Department)
{
    // ...
}
```
