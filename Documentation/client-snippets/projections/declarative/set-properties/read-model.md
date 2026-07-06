```csharp
public record DecSetPropsAccount(
    string AccountNumber,
    string CustomerName,
    decimal Balance,
    bool IsActive,
    DateTimeOffset OpenedAt,
    DateTimeOffset? LastTransaction);
```
