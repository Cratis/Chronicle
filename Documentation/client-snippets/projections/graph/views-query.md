```csharp
var engineers = await graph.Nodes<PersonWithGroups>()
    .WhereRelated<GroupMembership>(_ => _.Groups, groups => groups.Where(_ => _.Name, "Engineering"))
    .ToList();

var ada = await graph.Nodes<PersonWithGroups>()
    .Where(_ => _.Name, "Ada")
    .FirstOrDefault();
```
