---
agent: agent
description: Add a Chronicle projection to an existing read model slice.
---

# Add a Projection

I need to add a **Chronicle projection** that populates a read model from events.

> For **reactors** (automation / translation), use the `add-reactor` prompt instead.

## Inputs

- **Events to project from** — list the event types (e.g. `ProjectRegistered`, `ProjectRemoved`)
- **Read model** — paste the `record` type or describe the shape you want

## Projection rules (mandatory)

Follow `.github/instructions/vertical-slices.instructions.md` — projection section.

**Preferred — model-bound:** Place attributes directly on the read model record, no separate class needed.

```csharp
[ReadModel]
[FromEvent<ProjectRegistered>]
public record Project(
    [Key] ProjectId Id,
    ProjectName Name)
{
    public static ISubject<IEnumerable<Project>> AllProjects(IMongoCollection<Project> collection) =>
        collection.Observe();
}
```

**Alternative — fluent `IProjectionFor<T>:`** Use for complex joins, children, or conditionals.

```csharp
public class ProjectProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) =>
        builder
            .From<ProjectRegistered>(b =>
                b.UsingKey(e => e.ProjectId)
                 .Set(m => m.Name).To(e => e.Name))
            .RemovedWith<ProjectRemoved>();
}
```

**Critical rules:**
- AutoMap is on by default — just call `.From<>()` directly
- Joins are on Chronicle **events**, never on the read model
- Use `.RemovedWith<TEvent>()` for soft-delete events
- **There is NO `ProjectionId Identifier` property — do not add one**

## After creating the file

Run `dotnet build` and fix all errors before completing.
