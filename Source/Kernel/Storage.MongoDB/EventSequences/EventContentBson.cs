// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Converts event content without narrowing decimals or unsigned 64-bit integers.
/// </summary>
internal static class EventContentBson
{
    /// <summary>
    /// Parses event JSON using Decimal128 for decimal-formatted values and integers outside Int64's range.
    /// </summary>
    /// <param name="json">The serialized event content.</param>
    /// <param name="schema">The registered generation schema, when available.</param>
    /// <returns>The lossless BSON content.</returns>
    internal static BsonDocument FromJson(string json, JsonSchema? schema = null)
    {
        if (schema is not null)
        {
            return BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json), schema)!.ToJsonString());
        }
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.Number &&
                ((reader.TryGetUInt64(out var unsigned) && unsigned > long.MaxValue) ||
                 RequiresDecimal(Encoding.UTF8.GetString(reader.ValueSpan))))
            {
                return BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json))!.ToJsonString());
            }
        }
        return BsonDocument.Parse(json);
    }

    /// <summary>
    /// Renders Decimal128 values as ordinary JSON numbers, not extended JSON objects.
    /// </summary>
    /// <param name="document">The stored event content.</param>
    /// <returns>The event JSON for schema-based conversion and clients.</returns>
    internal static string ToJson(BsonDocument document) => ContainsDecimals(document)
        ? ToJsonObject(document).ToJsonString()
        : document.ToString();

    /// <summary>
    /// Restores content without serializing the restored tree again.
    /// </summary>
    /// <param name="document">The stored event content.</param>
    /// <returns>The content with ordinary decimal and unsigned JSON numbers.</returns>
    internal static JsonObject ToJsonObject(BsonDocument document)
    {
        var node = JsonNode.Parse(document.ToString())!.AsObject();
        return (JsonObject)RestoreDecimals(document, node)!;
    }

    static bool ContainsDecimals(BsonValue value) => value switch
    {
        BsonDecimal128 => true,
        BsonDocument document => document.Elements.Any(element => ContainsDecimals(element.Value)),
        BsonArray array => array.Any(ContainsDecimals),
        _ => false
    };

    static bool IsLargeUnsignedInteger(BsonDecimal128 number) =>
        number.Value > new Decimal128(long.MaxValue) &&
        number.Value <= new Decimal128(ulong.MaxValue) &&
        number.Value == new Decimal128(Decimal128.ToUInt64(number.Value));

    static JsonNode? PrepareForBson(JsonNode? node, JsonSchema? schema = null)
    {
        var actual = schema?.ActualTypeSchema;
        if (actual?.OneOf.Count > 0)
        {
            actual = actual.OneOf.FirstOrDefault(alternative => alternative.Type != JsonObjectType.Null)?.ActualTypeSchema ?? actual;
        }
        if (node is JsonObject document)
        {
            var properties = actual?.GetFlattenedProperties().ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
            return new JsonObject(document.Select(property => new KeyValuePair<string, JsonNode?>(
                property.Key,
                PrepareForBson(property.Value, properties?.GetValueOrDefault(property.Key) ?? actual?.AdditionalPropertiesSchema))));
        }
        if (node is JsonArray array)
        {
            return new JsonArray(array.Select(item => PrepareForBson(item, actual?.Item)).ToArray());
        }
        if (node is JsonValue value)
        {
            if ((actual?.Format ?? schema?.Format)?.TrimEnd('?') == "decimal" && value.GetValueKind() == JsonValueKind.Number)
            {
                return new JsonObject { ["$numberDecimal"] = value.ToJsonString() };
            }
            if (schema is null && value.GetValueKind() == JsonValueKind.Number && RequiresDecimal(value.ToJsonString()))
            {
                return new JsonObject { ["$numberDecimal"] = value.ToJsonString() };
            }
            if (value.TryGetValue<ulong>(out var unsigned) && unsigned > long.MaxValue)
            {
                return new JsonObject { ["$numberDecimal"] = unsigned.ToString(CultureInfo.InvariantCulture) };
            }
        }
        return node?.DeepClone();
    }

    static bool RequiresDecimal(string literal) =>
        !long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
        decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
        (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
         !string.Equals(literal, value.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal));

    static JsonNode? RestoreDecimals(BsonValue bson, JsonNode? node)
    {
        if (bson is BsonDecimal128 number)
        {
            try
            {
                return IsLargeUnsignedInteger(number)
                    ? JsonValue.Create(Decimal128.ToUInt64(number.Value))
                    : JsonValue.Create(Decimal128.ToDecimal(number.Value));
            }
            catch (OverflowException)
            {
                // Imported BSON can carry values outside CLR decimal's range, including non-finite values.
                return JsonValue.Create(number.Value.ToString());
            }
        }
        if (bson is BsonDocument document && node is JsonObject jsonObject)
        {
            foreach (var element in document)
            {
                var original = jsonObject[element.Name];
                var restored = RestoreDecimals(element.Value, original);
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
                var restored = RestoreDecimals(array[index], original);
                if (!ReferenceEquals(restored, original))
                {
                    jsonArray[index] = restored;
                }
            }
        }
        return node;
    }
}
