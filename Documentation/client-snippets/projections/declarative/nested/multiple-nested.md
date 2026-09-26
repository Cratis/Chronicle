```csharp title="Multiple nested objects"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record SliceCreatedWithMultipleNested(string Name);

[EventType]
public record CommandSetWithMultipleNested(string Name, string Schema);

[EventType]
public record CommandClearedWithMultipleNested;

[EventType]
public record ValidationConfiguredWithMultipleNested(string RuleName);

[EventType]
public record ValidationRemovedWithMultipleNested;

public record SliceWithMultipleNested(
    string Name,
    CommandItemWithMultipleNested? Command,
    ValidationConfigWithMultipleNested? Validation);

public record CommandItemWithMultipleNested(
    string Name,
    string Schema);

public record ValidationConfigWithMultipleNested(
    string RuleName);

public class SliceProjectionWithMultipleNested : IProjectionFor<SliceWithMultipleNested>
{
    public void Define(IProjectionBuilderFor<SliceWithMultipleNested> builder) => builder
        .From<SliceCreatedWithMultipleNested>()
        .Nested(m => m.Command, nested => nested
            .From<CommandSetWithMultipleNested>()
            // A later CommandSetWithMultipleNested recreates Command after it is cleared.
            .ClearWith<CommandClearedWithMultipleNested>())
        .Nested(m => m.Validation, nested => nested
            .From<ValidationConfiguredWithMultipleNested>()
            .ClearWith<ValidationRemovedWithMultipleNested>());
}
```

MongoDB documents written by older versions may contain explicit `null` values for cleared nested objects. Recreating a nested object at the document level repairs these legacy values automatically. Explicit `null` values **inside array elements** are not repaired automatically; replay the projection into a fresh collection or migrate those documents before recreating nested fields there.
