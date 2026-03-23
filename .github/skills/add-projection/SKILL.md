---
name: add-projection
description: Use this skill when asked to add a Chronicle projection to a Cratis-based project. Enforces the AutoMap-first rule and Chronicle-specific join semantics.
---

Add a Chronicle **projection** that populates a read model from events.

> For **reactors** (automation / translation), see the `add-reactor` skill instead.

## Projection — Model-Bound (preferred)

Put projection metadata directly on the read model using attributes. No separate class needed.

```csharp
[ReadModel]
[FromEvent<SomeEventHappened>]              // auto-maps all matching property names
public record <ReadModelName>(
    [Key] <IdType> Id,                      // marks the primary key
    <PropType> <PropName>)                  // auto-mapped from SomeEventHappened
{
    public static ISubject<IEnumerable<<ReadModelName>>> All(IMongoCollection<<ReadModelName>> collection) =>
        collection.Observe();
}
```

**Attribute reference:**
| Attribute | Purpose |
|-----------|---------|
| `[FromEvent<T>]` | Auto-maps all matching property names (equivalent to `.AutoMap().From<T>()`) |
| `[FromEvent<T>(key: nameof(T.Prop))]` | Same, but uses `Prop` as the read model key instead of EventSourceId |
| `[Key]` | Marks the primary key property |
| `[SetFrom<T>(nameof(T.Prop))]` | Explicitly maps one property from event T |
| `[AddFrom<T>(nameof(T.Prop))]` | Adds event property value to the read model property |
| `[SubtractFrom<T>(nameof(T.Prop))]` | Subtracts event property value |
| `[ChildrenFrom<T>(key: nameof(T.Prop))]` | Projects into a nested child collection |
| `[Join<T>(on: nameof(Prop), eventPropertyName: nameof(T.EProp))]` | Joins data from a related event |
| `[RemovedWith<T>]` | Marks the instance as removed when event T is appended |

**Critical rules:**
- Joins must be on Chronicle **events** — NEVER join on a read model type
- If property names between event and read model match, `[FromEvent<T>]` alone is sufficient
- Child types also support all attributes recursively

## Projection — Fluent (use for complex cases)

Use `IProjectionFor<T>` when projection logic is too complex for attributes.

```csharp
public class <Name>Projection : IProjectionFor<<ReadModel>>
{
    public void Define(IProjectionBuilderFor<<ReadModel>> builder) =>
        builder
            .From<SomeEventHappened>(b =>
                b.UsingKey(e => e.SomeId))
            .RemovedWith<SomeThingRemoved>();
}
```

**Critical rules:**
- AutoMap is on by default — just call `.From<>()` directly. Only call `.AutoMap()` if you previously used `.NoAutoMap()`.
- Joins are on Chronicle **events** only — NEVER join on the read model
- There is NO `Identifier` / `ProjectionId` property — do not add one

## After creating

Run `dotnet build`. Fix all errors before completing.

---

For the full model-bound projection attribute reference and fluent builder API, see [references/CHRONICLE-API.md](references/CHRONICLE-API.md).
