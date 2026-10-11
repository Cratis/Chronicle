```csharp
using Cratis.Chronicle.Events;

[EventType]
public record AccountOpened(string Owner, decimal InitialBalance);

[EventType]
public record DepositMade(decimal Amount);

[EventType]
public record WithdrawalMade(decimal Amount);
```
