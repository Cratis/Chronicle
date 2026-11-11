// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
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
    static readonly ConditionalWeakTable<JsonSchema, DecimalSchema> _decimalSchemas = new();
    static readonly ConditionalWeakTable<JsonSchema, Dictionary<string, JsonSchemaProperty>> _properties = new();

    /// <summary>
    /// Parses event JSON using Decimal128 for decimal-formatted values and integers outside Int64's range.
    /// </summary>
    /// <param name="json">The serialized event content.</param>
    /// <param name="schema">The registered generation schema, when available.</param>
    /// <returns>The lossless BSON content.</returns>
    internal static BsonDocument FromJson(string json, JsonSchema? schema = null)
    {
        if (schema is not null && _decimalSchemas.GetValue(schema, value => new(HasDecimal(value))).HasDecimal)
        {
            return BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json), schema)!.ToJsonString());
        }
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.Number &&
                ((reader.TryGetUInt64(out var unsigned) && unsigned > long.MaxValue) ||
                 RequiresDecimal(reader.ValueSpan)))
            {
                return BsonDocument.Parse(PrepareForBson(JsonNode.Parse(json), schema)!.ToJsonString());
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

    static bool HasDecimal(JsonSchema schema)
    {
        // Scan the finite schema document, including definitions, without resolving recursive references.
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(schema.ToJson()));
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName && reader.ValueTextEquals("format") &&
                reader.Read() && reader.TokenType is JsonTokenType.String && IsDecimalFormat(reader.GetString()))
            {
                return true;
            }
        }
        return false;
    }

    static bool IsDecimalFormat(string? format) => format?.TrimEnd('?') == "decimal";

    static bool ContainsDecimals(BsonValue value)
    {
        if (value is BsonDecimal128) return true;
        if (value is BsonDocument document)
        {
            foreach (var element in document)
            {
                if (ContainsDecimals(element.Value)) return true;
            }
        }
        else if (value is BsonArray array)
        {
            foreach (var item in array)
            {
                if (ContainsDecimals(item)) return true;
            }
        }
        return false;
    }

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
            var propertySchema = GetPropertySchema(schema, actual);
            var properties = propertySchema is null ? null : _properties.GetValue(
                propertySchema,
                static value => value.GetFlattenedProperties().ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase));
            return new JsonObject(document.Select(property => new KeyValuePair<string, JsonNode?>(
                property.Key,
                PrepareForBson(property.Value, properties?.GetValueOrDefault(property.Key) ?? actual?.AdditionalPropertiesSchema))));
        }
        if (node is JsonArray array)
        {
            return new JsonArray(array.Select(item => PrepareForBson(item, actual?.Item)).ToArray());
        }
        return node is JsonValue value ? PrepareValue(value, schema, actual) : node?.DeepClone();
    }

    static JsonSchema? GetPropertySchema(JsonSchema? schema, JsonSchema? actual)
    {
        var source = schema?.Reference ?? schema;
        // Flatten the complete object schema, not just the allOf branch selected as its actual type.
        return source?.AllOf.Count > 0 ? source : actual;
    }

    static JsonNode PrepareValue(JsonValue value, JsonSchema? schema, JsonSchema? actual)
    {
        if (value.GetValueKind() == JsonValueKind.Number &&
            (IsDecimalFormat(actual?.Format ?? schema?.Format) || (schema is null && RequiresDecimal(value.ToJsonString()))))
        {
            return new JsonObject { ["$numberDecimal"] = value.ToJsonString() };
        }
        if (value.TryGetValue<ulong>(out var unsigned) && unsigned > long.MaxValue)
        {
            return new JsonObject { ["$numberDecimal"] = unsigned.ToString(CultureInfo.InvariantCulture) };
        }
        return value.DeepClone();
    }

    static bool RequiresDecimal(ReadOnlySpan<byte> literal)
    {
        if (long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }
        if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return true;

        // JSON numbers are ASCII. Compare to the same round-trip format as the string path without decoding a token.
        Span<byte> formatted = stackalloc byte[32];
        return !value.TryFormat(formatted, out var written, "R", CultureInfo.InvariantCulture) ||
            !literal.SequenceEqual(formatted[..written]);
    }

    static bool RequiresDecimal(string literal) =>
        !long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
        decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
        (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
         !string.Equals(literal, value.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal));

    static JsonNode? RestoreDecimals(BsonValue bson, JsonNode? node)
    {
        if (bson is BsonDecimal128 number) return RestoreNumber(number);
        if (bson is BsonDocument document && node is JsonObject jsonObject)
        {
            RestoreDocument(document, jsonObject);
        }
        else if (bson is BsonArray array && node is JsonArray jsonArray)
        {
            RestoreArray(array, jsonArray);
        }
        return node;
    }

    static JsonValue? RestoreNumber(BsonDecimal128 number)
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

    static void RestoreDocument(BsonDocument document, JsonObject jsonObject)
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

    static void RestoreArray(BsonArray array, JsonArray jsonArray)
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

    sealed record DecimalSchema(bool HasDecimal);
}
