```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<CrudComparisonCustomerRegistered>]
[FromEvent<CrudComparisonAddressChanged>]
public record CrudComparisonCustomerCard(
    [Key] Guid Id,
    string Name,
    string Address,
    [Count<CrudComparisonAddressChanged>] int TimesRelocated);
```
