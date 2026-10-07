// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
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
    internal static BsonDocument FromJson(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.Number && reader.TryGetUInt64(out var unsigned) && unsigned > long.MaxValue)
            {
                return BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json))!.ToJsonString());
            }
        }

        return BsonDocument.Parse(json);
    }

    /// <summary>
    /// Render large unsigned integers as ordinary JSON numbers, not extended JSON objects.
    /// </summary>
    /// <param name="document">The stored event content.</param>
    /// <returns>The event JSON for schema-based conversion and clients.</returns>
    internal static string ToJson(BsonDocument document) => ContainsUnsignedIntegers(document)
        ? ToJsonObject(document).ToJsonString()
        : document.ToString();

    /// <summary>
    /// Restore stored content as a JSON object without serializing the restored tree again.
    /// </summary>
    /// <param name="document">The stored event content.</param>
    /// <returns>The event content with ordinary unsigned JSON numbers.</returns>
    internal static JsonObject ToJsonObject(BsonDocument document)
    {
        var node = JsonNode.Parse(document.ToString())!.AsObject();
        return (JsonObject)RestoreUnsignedIntegers(document, node)!;
    }

    static bool ContainsUnsignedIntegers(BsonValue value) => value switch
    {
        BsonDecimal128 number => IsLargeUnsignedInteger(number),
        BsonDocument document => document.Elements.Any(element => ContainsUnsignedIntegers(element.Value)),
        BsonArray array => array.Any(ContainsUnsignedIntegers),
        _ => false
    };

    static bool IsLargeUnsignedInteger(BsonDecimal128 number) =>
        number.Value > new Decimal128(long.MaxValue) &&
        number.Value <= new Decimal128(ulong.MaxValue) &&
        number.Value == new Decimal128(Decimal128.ToUInt64(number.Value));

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
        if (bson is BsonDecimal128 number && IsLargeUnsignedInteger(number))
        {
            return JsonValue.Create(Decimal128.ToUInt64(number.Value));
        }

        if (bson is BsonDocument document && node is JsonObject jsonObject)
        {
            foreach (var element in document)
            {
                var original = jsonObject[element.Name];
                var restored = RestoreUnsignedIntegers(element.Value, original);
                if (!ReferenceEquals(restored, original))
                {
                    jsonObject[element.Name] = restored;
                }
            }
        }
        else if (bson is BsonArray array && node is JsonArray jsonArray)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var original = jsonArray[index];
                var restored = RestoreUnsignedIntegers(array[index], original);
                if (!ReferenceEquals(restored, original))
                {
                    jsonArray[index] = restored;
                }
            }
        }

        return node;
    }
}
