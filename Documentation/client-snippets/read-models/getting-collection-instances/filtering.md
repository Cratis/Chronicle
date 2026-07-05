```csharp
var accounts = await eventStore.ReadModels.GetInstances<Account>();

var highValueAccounts = accounts
    .Where(account => account.Balance > threshold)
    .OrderByDescending(account => account.Balance)
    .ToList();

Console.WriteLine($"Found {highValueAccounts.Count} high-value accounts.");
```
