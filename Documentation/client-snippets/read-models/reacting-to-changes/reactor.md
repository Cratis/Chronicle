```csharp
using Cratis.Chronicle.ReadModels;

public class AccountNotifier : IReadModelReactor
{
    public Task Added(Account account) => SendWelcome(account);

    public Task Modified(Account account) => SendUpdated(account);

    public Task Removed(Account account) => SendClosed(account);

    Task SendWelcome(Account account) => Task.CompletedTask;
    Task SendUpdated(Account account) => Task.CompletedTask;
    Task SendClosed(Account account) => Task.CompletedTask;
}
```
