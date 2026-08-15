```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0048SliceCommandCleared();

[EventType]
public record Chr0048SliceCommandSet(string Command, int Attempts);

public record Chr0048Slice(
    [Key] string Id,

    // Error CHR0048: 'Command' is declared as a non-nullable string, so there is no value the clear
    // could write except the empty string - a different fact the read model cannot tell apart from a
    // real value. Declare it as string? to clear it, or say what you mean with [SetValue<T>("")].
    [SetFrom<Chr0048SliceCommandSet>(nameof(Chr0048SliceCommandSet.Command))]
    [ClearWith<Chr0048SliceCommandCleared>]
    string Command,

    // Error CHR0048: a value type cannot hold null whatever the nullable context says, and zero is a
    // count rather than the absence of one. Declare it as int? to clear it.
    [SetFrom<Chr0048SliceCommandSet>(nameof(Chr0048SliceCommandSet.Attempts))]
    [SetValue<Chr0048SliceCommandCleared>(null)]
    int Attempts);

// The declarations that are correct: both members can hold the value a clear writes, so both spellings
// of the clear are accepted and the member really does go back to no value - replay included.
public record Chr0048SliceFixed(
    [Key] string Id,

    [SetFrom<Chr0048SliceCommandSet>(nameof(Chr0048SliceCommandSet.Command))]
    [ClearWith<Chr0048SliceCommandCleared>]
    string? Command,

    [SetFrom<Chr0048SliceCommandSet>(nameof(Chr0048SliceCommandSet.Attempts))]
    [SetValue<Chr0048SliceCommandCleared>(null)]
    int? Attempts);
```
