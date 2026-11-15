```csharp
[Node]
[FromEvent<GroupCreated>]
public record Group(
    [Key] Guid GroupId,
    string Name,
    [Increment<MemberAdded>] [Decrement<MemberRemoved>] int MemberCount);
```
