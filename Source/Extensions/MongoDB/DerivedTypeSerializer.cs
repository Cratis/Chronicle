// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a <see cref="SerializerBase{T}"/> for serializing and deserializing types that hold the <see cref="DerivedTypeAttribute"/>.
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public class DerivedTypeSerializer<T> : SerializerBase<T>
{
    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeSerializer{T}"/> class.
    /// </summary>
    /// <param name="options"><see cref="JsonSerializerOptions"/> to use.</param>
    public DerivedTypeSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        var documentAsJson = JsonSerializer.Serialize(value, _options);
        var document = BsonSerializer.Deserialize<BsonDocument>(documentAsJson);
        var serializer = BsonSerializer.LookupSerializer<BsonDocument>();
        serializer.Serialize(context, document.AsBsonDocument);
    }

    /// <inheritdoc/>
    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var json = document["_v"].ToJson();
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}
