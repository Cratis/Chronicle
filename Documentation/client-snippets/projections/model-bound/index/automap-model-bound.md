```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<MbIndexAutoMapAccountOpened>]
public record MbIndexAutoMapMbAccountInfo(
    [Key] Guid Id,
    string Name,        // Automatically mapped from MbIndexAutoMapAccountOpened.Name
    decimal Balance);   // Automatically mapped from MbIndexAutoMapAccountOpened.Balance
```
