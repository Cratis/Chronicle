```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record StableIdentityAccountOpened(string Name);

// Before a rename, this read model was Accounts.AccountInfo.
// Keep its previous projection ID while renaming the CLR type.
[Projection(id: "Accounts.AccountInfo")]
[FromEvent<StableIdentityAccountOpened>]
public record StableIdentityRenamedAccountInfo([Key] Guid Id, string Name);
```
