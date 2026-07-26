```csharp
using Cratis.Chronicle.ReadModels;

[Materialized]
public class AccountSnapshotReactor : IReadModelReactor
{
    public Task Added(Account account) => Task.CompletedTask;
}
```
