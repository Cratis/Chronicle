```csharp
[ViewOf<Person>]
public record PersonWithGroups(
    [Key] Guid Id,
    string Name,
    [Follows<MemberOf>] IReadOnlyList<GroupMembership> Groups);

[ViewOf<Group>]
public record GroupMembership(Guid Id, string Name, [FromEdge] DateTimeOffset Since);
```
