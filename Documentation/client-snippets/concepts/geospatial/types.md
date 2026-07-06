```csharp
using Cratis.Geospatial;

public static class GeospatialTypeExamples
{
    public static Point CreatePoint() => new(10.456, 42.123);

    public static LineString CreatePath() => new([
        new Point(10.456, 42.123),
        new Point(11.789, 43.456)
    ]);

    public static Polygon CreateBoundary() => new(
        Shell: new LinearRing([
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        ]),
        Holes: []);
}
```
