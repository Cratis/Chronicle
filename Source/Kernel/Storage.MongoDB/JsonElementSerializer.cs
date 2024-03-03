// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Orleans.Hosting;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="JsonElement"/>.
/// </summary>
public class JsonElementSerializer : SerializerBase<JsonElement>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonElement value)
    {
        var document = BsonDocument.Parse(value.GetRawText());
        var serializer = BsonSerializer.LookupSerializer<BsonDocument>();
        serializer.Serialize(context, document);
    }

    /// <inheritdoc/>
    public override JsonElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var serializer = BsonSerializer.LookupSerializer<BsonDocument>();
        var document = serializer.Deserialize(context, args);
        return JsonSerializer.Deserialize<JsonElement>(document.ToJson(), Globals.JsonSerializerOptions);
    }
}
