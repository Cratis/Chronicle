```csharp
using var scenario = new GraphScenario();

await scenario.Append(personId, new PersonRegistered(personId, "Ada"));
await scenario.Append(personId, new MemberAdded(personId, groupId));

var person = await scenario.Node<Person>(personId);
var membership = await scenario.Edge<MemberOf>(personId, groupId);
var memberships = await scenario.Edges<MemberOf>();
```
