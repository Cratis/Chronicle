```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecSetOnceDocumentRevised(string Title);

public record DecSetOnceDocument(string OriginalTitle, string Title, string CreatedBy, DateTimeOffset Created, DateTimeOffset LastRevised);

public class DecSetOnceDocumentProjection : IProjectionFor<DecSetOnceDocument>
{
    public void Define(IProjectionBuilderFor<DecSetOnceDocument> builder) => builder
        .From<DecSetOnceDocumentRevised>(_ => _
            .SetOnce(m => m.OriginalTitle).To(e => e.Title)
            .Set(m => m.Title).To(e => e.Title)
            .SetOnce(m => m.CreatedBy).ToEventContextProperty(c => c.CausedBy.Subject)
            .SetOnce(m => m.Created).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.LastRevised).ToEventContextProperty(c => c.Occurred));
}
```
