```csharp title="Set a value only once"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record DocumentRevisedForSetOnce(string Title);

public record SetOnceDocument(
    [Key] Guid Id,

    [SetFrom<DocumentRevisedForSetOnce>(nameof(DocumentRevisedForSetOnce.Title), SetOnce = true)]
    string OriginalTitle,

    [SetFrom<DocumentRevisedForSetOnce>]
    string Title,

    [SetFromContext<DocumentRevisedForSetOnce>(nameof(EventContext.Occurred), SetOnce = true)]
    DateTimeOffset Created);
```
