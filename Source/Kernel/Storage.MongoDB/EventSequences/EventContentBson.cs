// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Converts event JSON to BSON without narrowing unsigned 64-bit integers.
/// </summary>
internal static class EventContentBson
{
    /// <summary>
    /// Parse event JSON, using Decimal128 only for integers outside Int64's range.
    /// </summary>
    /// <param name="json">The serialized event content.</param>
    /// <returns>The lossless BSON content.</returns>
    internal static BsonDocument FromJson(string json) => BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json))!.ToJsonString());

    /// <summary>
    /// Render large unsigned integers as ordinary JSON numbers, not extended JSON objects.
    /// </summary>
    /// <param name="document">The stored event content.</param>
    /// <returns>The event JSON for schema-based conversion and clients.</returns>
    internal static string ToJson(BsonDocument document) => RestoreUnsignedIntegers(document, JsonNode.Parse(document.ToString()))!.ToJsonString();

    static JsonNode? PrepareForBson(JsonNode? node) => node switch
    {
        null => null,
        JsonObject document => new JsonObject(document.Select(property => new KeyValuePair<string, JsonNode?>(property.Key, PrepareForBson(property.Value)))),
        JsonArray array => new JsonArray(array.Select(PrepareForBson).ToArray()),
        JsonValue value when value.TryGetValue<ulong>(out var unsigned) && unsigned > long.MaxValue =>
            new JsonObject { ["$numberDecimal"] = unsigned.ToString(CultureInfo.InvariantCulture) },
        _ => node.DeepClone()
    };

    static JsonNode? RestoreUnsignedIntegers(BsonValue bson, JsonNode? node)
    {
        if (bson is BsonDecimal128 number && number.Value > new Decimal128(long.MaxValue) && number.Value <= new Decimal128(ulong.MaxValue) && number.Value == new Decimal128(Decimal128.ToUInt64(number.Value)))
        {
            return JsonValue.Create(Decimal128.ToUInt64(number.Value));
        }

        if (bson is BsonDocument document && node is JsonObject jsonObject)
        {
            foreach (var element in document)
            {
                jsonObject[element.Name] = RestoreUnsignedIntegers(element.Value, jsonObject[element.Name]);
            }
        }
        else if (bson is BsonArray array && node is JsonArray jsonArray)
        {
            for (var index = 0; index < array.Count; index++)
            {
                jsonArray[index] = RestoreUnsignedIntegers(array[index], jsonArray[index]);
            }
        }

        return node;
    }
}
