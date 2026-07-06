```csharp title="Read model with nested object"
public record SliceWithNestedCommand(
    string Name,
    CommandItemForNestedCommand? Command);

public record CommandItemForNestedCommand(
    string Name,
    string Schema);
```
