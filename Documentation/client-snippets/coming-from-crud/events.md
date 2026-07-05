```csharp
using Cratis.Chronicle.Events;

[EventType]
public record CrudComparisonCustomerRegistered(string Name, string Address);

[EventType]
public record CrudComparisonAddressChanged(string Address);
```
