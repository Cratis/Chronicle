```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Chr0033Note(string Text);

[EventType]
public record Chr0033LineAdded(string LineNumber, string Description, IReadOnlyList<Chr0033Note> Annotations);

public record Chr0033Line(
    [Key] string LineNumber,
    string Description,
    IReadOnlyList<Chr0033Note> Notes);

public record Chr0033Order(
    [Key] Guid Id,

    // Warning CHR0033: the child property 'Notes' matches no property on Chr0033LineAdded (the event
    // carries 'Annotations'), so AutoMap fills it from nothing and it always projects as empty.
    [ChildrenFrom<Chr0033LineAdded>(key: nameof(Chr0033LineAdded.LineNumber))]
    IEnumerable<Chr0033Line> Lines);
```
