// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a BSON serializer for <see cref="LineString"/>.
/// </summary>
/// <remarks>
/// Stores the line string as an array of points: <c>[{ "longitude": &lt;double&gt;, "latitude": &lt;double&gt; }, ...]</c>.
/// </remarks>
public class LineStringSerializer : SerializerBase<LineString>
{
    const string LongitudeField = "longitude";
    const string LatitudeField = "latitude";

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LineString value)
    {
        var array = new BsonArray();
        foreach (var point in value.Coordinates)
        {
            var document = new BsonDocument
            {
                [LongitudeField] = point.Longitude,
                [LatitudeField] = point.Latitude
            };
            array.Add(document);
        }
        BsonArraySerializer.Instance.Serialize(context, args, array);
    }

    /// <inheritdoc/>
    public override LineString Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var array = BsonArraySerializer.Instance.Deserialize(context, args);
        var points = new Point[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            var document = array[i] as BsonDocument ?? throw new InvalidOperationException("Expected BsonDocument in LineString array");
            var longitude = document.TryGetValue(LongitudeField, out var lon) ? lon.AsDouble : 0d;
            var latitude = document.TryGetValue(LatitudeField, out var lat) ? lat.AsDouble : 0d;
            points[i] = new Point(longitude, latitude);
        }
        return new LineString(points);
    }
}
