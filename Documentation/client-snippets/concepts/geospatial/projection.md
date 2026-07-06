```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Geospatial;

[EventType]
public record GeospatialProjAssetLocationUpdated(Point Location);

public record GeospatialProjAssetReadModel(Guid Id, Point Location);

public class GeospatialProjAssetProjection : IProjectionFor<GeospatialProjAssetReadModel>
{
    // AutoMap picks up the Point-typed property automatically — no manual mapping needed
    public void Define(IProjectionBuilderFor<GeospatialProjAssetReadModel> builder) => builder
        .From<GeospatialProjAssetLocationUpdated>();
}
```
