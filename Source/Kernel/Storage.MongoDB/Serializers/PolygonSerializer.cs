// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a BSON serializer for <see cref="Polygon"/>.
/// </summary>
/// <remarks>
/// Stores the polygon with a shell (outer ring) and optional holes (inner rings):
/// <c>{ "shell": [{ "longitude": &lt;double&gt;, "latitude": &lt;double&gt; }, ...], "holes": [[...], ...] }</c>.
/// </remarks>
public class PolygonSerializer : SerializerBase<Polygon>
{
    const string LongitudeField = "longitude";
    const string LatitudeField = "latitude";
    const string ShellField = "shell";
    const string HolesField = "holes";

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Polygon value)
    {
        var shellArray = SerializeRing(value.Shell.Coordinates);
        var holesArray = new BsonArray();
        foreach (var hole in value.Holes)
        {
            holesArray.Add(SerializeRing(hole.Coordinates));
        }

        var document = new BsonDocument
        {
            [ShellField] = shellArray,
            [HolesField] = holesArray
        };

        BsonDocumentSerializer.Instance.Serialize(context, args, document);
    }

    /// <inheritdoc/>
    public override Polygon Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
        var shellArray = document.TryGetValue(ShellField, out var shellValue) ? shellValue as BsonArray : new BsonArray();
        var holesArray = document.TryGetValue(HolesField, out var holesValue) ? holesValue as BsonArray : new BsonArray();

        var shell = new LinearRing(DeserializeRing(shellArray ?? new BsonArray()));
        var holes = new LinearRing[holesArray?.Count ?? 0];
        for (var i = 0; i < holes.Length; i++)
        {
            var holeArray = holesArray![i] as BsonArray ?? new BsonArray();
            holes[i] = new LinearRing(DeserializeRing(holeArray));
        }

        return new Polygon(shell, holes);
    }

    static BsonArray SerializeRing(Point[] coordinates)
    {
        var ringArray = new BsonArray();
        foreach (var point in coordinates)
        {
            ringArray.Add(new BsonDocument
            {
                [LongitudeField] = point.Longitude,
                [LatitudeField] = point.Latitude
            });
        }
        return ringArray;
    }

    static Point[] DeserializeRing(BsonArray ringArray)
    {
        var points = new Point[ringArray.Count];
        for (var i = 0; i < ringArray.Count; i++)
        {
            var document = ringArray[i] as BsonDocument ?? throw new InvalidOperationException("Expected BsonDocument in polygon ring");
            var longitude = document.TryGetValue(LongitudeField, out var lon) ? lon.AsDouble : 0d;
            var latitude = document.TryGetValue(LatitudeField, out var lat) ? lat.AsDouble : 0d;
            points[i] = new Point(longitude, latitude);
        }
        return points;
    }
}
