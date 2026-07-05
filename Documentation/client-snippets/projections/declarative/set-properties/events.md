```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecSetPropsAccountOpened(
    string Number,
    DecSetPropsCustomer Owner,
    DateTimeOffset Timestamp);

[EventType]
public record DecSetPropsMoneyDeposited(
    decimal Amount,
    DateTimeOffset Timestamp);

public record DecSetPropsCustomer(string Name, string Email);
```
