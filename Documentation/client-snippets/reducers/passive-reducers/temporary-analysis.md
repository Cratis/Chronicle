```csharp
using Cratis.Chronicle.ReadModels;

[Passive]
public record PassiveReducersCustomerBehaviorAnalysis(
    int UniqueCustomers,
    decimal AverageOrderValue,
    Dictionary<int, int> OrdersByHour);
```
