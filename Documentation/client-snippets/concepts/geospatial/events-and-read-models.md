```csharp
using Cratis.Chronicle.Events;
using Cratis.Geospatial;

[EventType]
public record GeospatialAssetLocationUpdated(Point Location);

[EventType]
public record GeospatialRouteCreated(LineString Path);

[EventType]
public record GeospatialZoneEstablished(Polygon Boundaries);

public record GeospatialAssetReadModel(Guid Id, Point Location);
public record GeospatialRouteReadModel(Guid Id, LineString Path);
public record GeospatialZoneReadModel(Guid Id, Polygon Boundaries);
```
