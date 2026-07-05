```csharp title="Read model with children"
public record GroupWithMembers(
    string Name,
    string Description,
    IEnumerable<GroupMember> Members);

public record GroupMember(
    string UserId,
    string Role);
```
