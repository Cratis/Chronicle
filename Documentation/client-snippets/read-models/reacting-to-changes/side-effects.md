```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[EventType]
public record AccountFlagged(string AccountId);

public class AccountReviewer : IReadModelReactor
{
    public Task<AccountFlagged> Modified(Account account) =>
        Task.FromResult(new AccountFlagged(account.Id));
}
```
