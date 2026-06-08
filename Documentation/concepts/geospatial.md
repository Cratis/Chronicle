# Geospatial Coordinates

Chronicle has built-in support for the `Coordinate` type from Cratis.Fundamentals. You can use it directly in events, read models, reducers, and projections without any extra setup.

## The Coordinate type

`Coordinate` is a record defined in the `Cratis.Geospatial` namespace:

```csharp
public record Coordinate(double Longitude, double Latitude);
```

It represents a geographic point in standard longitude-then-latitude order (the order used by GeoJSON and most geospatial standards).

## Using Coordinate in events and read models

Import the namespace and use `Coordinate` as any other property type:

```csharp
using Cratis.Geospatial;

[EventType]
public record AssetLocationUpdated(AssetId AssetId, Coordinate Location);

public record AssetReadModel(AssetId AssetId, Coordinate Location);
```

Because `Coordinate` is a plain record, projections and reducers handle it like any other value type. AutoMap picks it up automatically:

```csharp
public class AssetProjection : IProjectionFor<AssetReadModel>
{
    public void Define(IProjectionBuilderFor<AssetReadModel> builder) => builder
        .AutoMap()
        .From<AssetLocationUpdated>(fb => fb
            .Set(m => m.Location).To(e => e.Location));
}
```

## JSON schema

When Chronicle generates the JSON schema for an event or read model that contains a `Coordinate` property, the property is annotated with `"format": "coordinate"`. This lets consumers inspect the schema and know the field is a geographic coordinate without inspecting the nested `longitude`/`latitude` sub-properties:

```json
{
  "properties": {
    "location": {
      "type": "object",
      "format": "coordinate",
      "properties": {
        "longitude": { "type": "number" },
        "latitude":  { "type": "number" }
      }
    }
  }
}
```

## Storage

### MongoDB sink

Chronicle stores `Coordinate` values as a flat BSON document with `longitude` and `latitude` fields:

```json
{ "longitude": 10.456, "latitude": 42.123 }
```

The BSON serializer is registered automatically — no configuration required.

### SQL sink

Because `Coordinate` generates as a JSON object in the schema, the SQL sink stores it as a JSON column. No migration or configuration changes are required.

## Workbench display

The Chronicle Workbench recognizes `"format": "coordinate"` in event schemas and renders coordinate fields as a human-readable string rather than expanding the nested object:

```text
lat: 42.123, long: 10.456
```
