```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[PII]
public record ComplianceReadModelsPersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ComplianceReadModelsPersonName NotSet = new(string.Empty);
    public static implicit operator string(ComplianceReadModelsPersonName name) => name.Value;
    public static implicit operator ComplianceReadModelsPersonName(string value) => new(value);
}

[EventType]
public record ComplianceReadModelsEmployeeRegistered(ComplianceReadModelsPersonName Name, string Department);

[FromEvent<ComplianceReadModelsEmployeeRegistered>]
public record ComplianceReadModelsEmployee(
    [Key] Guid Id,
    [PII] string Name,  // mapped from ComplianceReadModelsPersonName; [PII] here is what stores it encrypted
    string Department);
```
