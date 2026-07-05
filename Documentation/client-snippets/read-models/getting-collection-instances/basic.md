```csharp
var accounts = await eventStore.ReadModels.GetInstances<Account>();

foreach (var account in accounts)
{
    Console.WriteLine($"{account.Name}: {account.Balance:C}");
}
```
