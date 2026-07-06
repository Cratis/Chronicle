```csharp
AccountInfo? account = await eventStore.ReadModels.GetInstanceById<AccountInfo>(accountId);

if (account is not null)
{
    Console.WriteLine($"{account.Name}: {account.Balance:C}");
}
```
