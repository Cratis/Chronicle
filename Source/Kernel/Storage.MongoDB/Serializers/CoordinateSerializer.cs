// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a BSON serializer for <see cref="Coordinate"/>.
/// </summary>
/// <remarks>
/// Stores the coordinate as a document: <c>{ "longitude": &lt;double&gt;, "latitude": &lt;double&gt; }</c>.
/// </remarks>
public class CoordinateSerializer : SerializerBase<Coordinate>
{
    const string LongitudeField = "longitude";
    const string LatitudeField = "latitude";

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Coordinate value)
    {
        var document = new BsonDocument
        {
            [LongitudeField] = value.Longitude,
            [LatitudeField] = value.Latitude
        };
        BsonDocumentSerializer.Instance.Serialize(context, args, document);
    }

    /// <inheritdoc/>
    public override Coordinate Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
        var longitude = document.TryGetValue(LongitudeField, out var lon) ? lon.AsDouble : 0d;
        var latitude = document.TryGetValue(LatitudeField, out var lat) ? lat.AsDouble : 0d;
        return new Coordinate(longitude, latitude);
    }
}
