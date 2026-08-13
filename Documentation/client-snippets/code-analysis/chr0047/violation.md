```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0047SliceCommandCleared();

[EventType]
public record Chr0047SliceCommandSet(string Command);

public record Chr0047Slice(
    [Key] string Id,

    // Warning CHR0047: a null value is skipped when the set-value mappings are built, so no
    // mapping is emitted for the member at all. The member still counts as explicitly mapped,
    // which also suppresses CHR0024. 'Command' keeps its last value forever, replay included.
    [SetFrom<Chr0047SliceCommandSet>(nameof(Chr0047SliceCommandSet.Command))]
    [SetValue<Chr0047SliceCommandCleared>(null)]
    string? Command,

    // Warning CHR0047: [ClearWith] is only read from the class-level attributes of a nested
    // single-object type, never from a property or a parameter. This one binds to nothing.
    [ClearWith<Chr0047SliceCommandCleared>]
    string? Note);

// The one shape that works: [ClearWith] declared on the nested type itself, clearing the whole
// nested object. There is no scalar equivalent today.
[ClearWith<Chr0047SliceCommandCleared>]
public record Chr0047SliceCommand(string Name);

public record Chr0047SliceWithNested(
    [Key] string Id,
    [Nested] Chr0047SliceCommand? Command);
```
