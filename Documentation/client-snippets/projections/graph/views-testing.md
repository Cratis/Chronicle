```csharp
using var scenario = new GraphScenario();

await scenario.Append(personId, new PersonRegistered(personId, "Ada"));
await scenario.Append(groupId, new GroupCreated(groupId, "Engineering"));
await scenario.Append(personId, new MemberAdded(personId, groupId));

var people = await scenario.Graph.Nodes<PersonWithGroups>().ToList();
```
