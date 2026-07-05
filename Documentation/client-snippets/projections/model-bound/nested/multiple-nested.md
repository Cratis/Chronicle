```csharp title="Multiple nested objects on one parent"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedMultiple(string Name, string Schema);

[EventType]
public record CommandClearedForNestedMultiple;

[EventType]
public record ValidationConfiguredForNestedMultiple(string Rules, bool IsStrict);

[EventType]
public record ValidationRemovedForNestedMultiple;

public record SliceWithMultipleNestedObjects(
    string Name,
    [Nested] CommandItemNestedMultiple? Command,
    [Nested] ValidationConfigNestedMultiple? Validation);

[FromEvent<CommandSetForNestedMultiple>]
[ClearWith<CommandClearedForNestedMultiple>]
public record CommandItemNestedMultiple(string Name, string Schema);

[FromEvent<ValidationConfiguredForNestedMultiple>]
[ClearWith<ValidationRemovedForNestedMultiple>]
public record ValidationConfigNestedMultiple(string Rules, bool IsStrict);
```
