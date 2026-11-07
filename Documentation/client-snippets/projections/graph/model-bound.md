```csharp
[Graph("Organization")]
[Node]
[FromEvent<PersonRegistered>]
[RemovedWith<PersonRemoved>(detach: true)]
public record Person([Key] Guid PersonId, string Name);

[Graph("Organization")]
[Node]
[FromEvent<GroupCreated>]
public record Group([Key] Guid GroupId, string Name);

[Graph("Organization")]
[Edge("MEMBER_OF")]
[EdgeFrom<MemberAdded>(sourceKey: nameof(MemberAdded.PersonId), targetKey: nameof(MemberAdded.GroupId))]
[EdgeRemovedWith<MemberRemoved>(sourceKey: nameof(MemberRemoved.PersonId), targetKey: nameof(MemberRemoved.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<MemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);
```
