```csharp title="Initialize collections"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public record InitialValuesAddress(string Street, string City);

[EventType]
public record InitialValuesCustomerRegistered(string Name);

public record InitialValuesCustomerRecord(
    string Name,
    IEnumerable<InitialValuesAddress> Addresses,
    IEnumerable<string> Tags);

public class InitialValuesCustomerRecordProjection : IProjectionFor<InitialValuesCustomerRecord>
{
    public void Define(IProjectionBuilderFor<InitialValuesCustomerRecord> builder) => builder
        .WithInitialValues(() => new InitialValuesCustomerRecord(
            Name: string.Empty,
            Addresses: Array.Empty<InitialValuesAddress>(),
            Tags: Array.Empty<string>()))
        .From<InitialValuesCustomerRegistered>();
}
```
