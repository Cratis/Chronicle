```csharp title="Multiple set mappings"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AccountOpenedForRename(string AccountName);

[EventType]
public record AccountRenamedForRename(string NewName);

public record RenameableAccount(
    [Key] Guid Id,

    [SetFrom<AccountOpenedForRename>(nameof(AccountOpenedForRename.AccountName))]
    [SetFrom<AccountRenamedForRename>(nameof(AccountRenamedForRename.NewName))]
    string Name);
```
