```csharp title="Nested property on the parent"
using Cratis.Chronicle.Projections.ModelBound;

public record ParentWithNestedProperty(
    [Nested] NestedPropertyChild? Child);

public record NestedPropertyChild(
    string Name,
    string Description);
```
