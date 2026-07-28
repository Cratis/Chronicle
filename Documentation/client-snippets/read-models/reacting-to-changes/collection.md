```csharp
using Cratis.Chronicle.ReadModels;

public class AccountBatchProjector : IReadModelReactor
{
    public async Task Modified(IEnumerable<Account> accounts)
    {
        foreach (var account in accounts)
        {
            await Sync(account);
        }
    }

    Task Sync(Account account) => Task.CompletedTask;
}
```
