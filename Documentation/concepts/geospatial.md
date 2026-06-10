# Geospatial Types

Chronicle has built-in support for geospatial types from Cratis.Fundamentals: `Point`, `LineString`, and `Polygon`. You can use them directly in events, read models, reducers, and projections without any extra setup.

## Geospatial Types

The three geospatial types are records defined in the `Cratis.Geospatial` namespace:

```csharp
public record Point(double Longitude, double Latitude);

public record LineString(Point[] Coordinates);

public record Polygon(LinearRing Shell, LinearRing[] Holes);

public record LinearRing(Point[] Coordinates);
```

- **Point** represents a single geographic location in longitude-then-latitude order (GeoJSON standard).
- **LineString** represents a connected series of points forming a line or path.
- **Polygon** represents an enclosed area with a shell (outer boundary) and optional holes (inner boundaries).
- **LinearRing** is an array of points that form a closed loop (used by Polygon).

## Using Geospatial Types in Events and Read Models

Import the namespace and use the geospatial types as any other property type:

```csharp
using Cratis.Geospatial;

[EventType]
public record AssetLocationUpdated(AssetId AssetId, Point Location);

[EventType]
public record RouteCreated(RouteId RouteId, LineString Path);

[EventType]
public record ZoneEstablished(ZoneId ZoneId, Polygon Boundaries);

public record AssetReadModel(AssetId AssetId, Point Location);
public record RouteReadModel(RouteId RouteId, LineString Path);
public record ZoneReadModel(ZoneId ZoneId, Polygon Boundaries);
```

Projections and reducers handle geospatial types like any other value type. AutoMap picks them up automatically:

```csharp
public class AssetProjection : IProjectionFor<AssetReadModel>
{
    public void Define(IProjectionBuilderFor<AssetReadModel> builder) => builder
        .AutoMap()
        .From<AssetLocationUpdated>(fb => fb
            .Set(m => m.Location).To(e => e.Location));
}
```

## JSON Schema

When Chronicle generates the JSON schema, geospatial types are annotated with their GeoJSON-compatible formats:

**Point**: Generates as an object with `longitude` and `latitude` properties, marked with `"format": "point"`

```json
{
  "type": "object",
  "format": "point",
  "properties": {
    "longitude": { "type": "number" },
    "latitude": { "type": "number" }
  }
}
```

**LineString**: Generates as an array of point objects, marked with `"format": "linestring"`

```json
{
  "type": "array",
  "format": "linestring",
  "items": {
    "type": "object",
    "properties": {
      "longitude": { "type": "number" },
      "latitude": { "type": "number" }
    }
  }
}
```

**Polygon**: Generates as an object with `shell` and `holes`, marked with `"format": "polygon"`

```json
{
  "type": "object",
  "format": "polygon",
  "properties": {
    "shell": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "longitude": { "type": "number" },
          "latitude": { "type": "number" }
        }
      }
    },
    "holes": {
      "type": "array",
      "items": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "longitude": { "type": "number" },
            "latitude": { "type": "number" }
          }
        }
      }
    }
  }
}
```

This lets consumers inspect the schema and know the field contains geographic data, and the format annotations make it possible for tools like the Workbench to recognize and properly display geographic values.

## Storage

### MongoDB Sink

Chronicle stores geospatial values in BSON format:

**Point**: Flat document with `longitude` and `latitude`

```json
{ "longitude": 10.456, "latitude": 42.123 }
```

**LineString**: Array of points

```json
[
  { "longitude": 10.456, "latitude": 42.123 },
  { "longitude": 11.789, "latitude": 43.456 }
]
```

**Polygon**: Document with `shell` (outer ring) and `holes` (inner rings)

```json
{
  "shell": [
    { "longitude": 0, "latitude": 0 },
    { "longitude": 10, "latitude": 0 },
    { "longitude": 10, "latitude": 10 },
    { "longitude": 0, "latitude": 10 },
    { "longitude": 0, "latitude": 0 }
  ],
  "holes": []
}
```

BSON serializers are registered automatically — no configuration required.

### SQL Sink

Geospatial types generate as JSON objects in the schema, so the SQL sink stores them as JSON columns. No migration or configuration changes are required.

## Workbench Display

The Chronicle Workbench recognizes geospatial format markers in event schemas and renders them as human-readable strings rather than expanding nested structures:

```text
Point: lat: 42.123, long: 10.456
LineString: [lat: 42.123, long: 10.456], [lat: 43.456, long: 11.789]
Polygon: shell with 5 points, 0 holes
```
