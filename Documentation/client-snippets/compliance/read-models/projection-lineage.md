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
    string Name,        // mapped from ComplianceReadModelsPersonName — stored encrypted at rest
    string Department);
```
