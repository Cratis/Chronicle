// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Json;
using Cratis.Types;

namespace Cratis.Chronicle.Json;

public partial class ExpandoObjectConverter
{
    static object ConvertToDecimal(Type targetType, object input) => input switch
    {
        double value => DecimalValues.FromDouble(value),
        float value => DecimalValues.FromSingle(value),
        _ => TypeConversion.Convert(targetType, input)
    };

    JsonNode? ConvertToJsonNode(object? value, JsonSchema schemaProperty)
    {
        // Compliance handlers replace protected scalar values with opaque strings while the registered schema
        // intentionally remains the schema of the plaintext event. Keep those strings opaque until the
        // compliance manager releases them instead of coercing ciphertext through the plaintext scalar type.
        if (value is string compliantValue && ConverterSchemas.HasProtectedValueMetadata(schemaProperty))
        {
            return JsonValue.Create(compliantValue);
        }

        if (schemaProperty.IsDictionary)
        {
            return UnknownValueConverter.ToJsonNode(value);
        }

        if (value is ExpandoObject expando)
        {
            return ToJsonObject(expando, ConverterSchemas.ObjectSchemaOf(schemaProperty));
        }

        if (schemaProperty.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable)
        {
            return ConvertArrayValueToJsonNode(value, schemaProperty);
        }

        if (typeFormats.IsKnown(ConverterSchemas.FormatOf(schemaProperty)))
        {
            return ConvertFormattedValueToJsonNode(value, schemaProperty);
        }

        // Fall back to schema-agnostic conversion for collection types that didn't match
        // the schema-typed array branch above (e.g. when NJsonSchema doesn't set Type=Array).
        if (value is IEnumerable and not string)
        {
            return UnknownValueConverter.ToJsonNode(value);
        }

        return SchemaScalarConverter.ToJsonNode(value, schemaProperty);
    }

    JsonNode ConvertArrayValueToJsonNode(object value, JsonSchema schemaProperty)
    {
        // A coarse [PII] value on a whole list/array is blob-encrypted to a single ciphertext string,
        // even though the schema type stays Array. Serialize it as that scalar string rather than letting
        // the enumeration treat the string as an enumerable of characters — which would shred the
        // ciphertext into per-character elements and break the encrypt -> store -> read -> release round trip.
        if (value is string ciphertext)
        {
            return JsonValue.Create(ciphertext);
        }

        var items = new List<JsonNode?>();
        var itemSchema = schemaProperty.Item?.Reference ?? schemaProperty.Item;
        foreach (var item in (IEnumerable)value)
        {
            items.Add(itemSchema is not null
                ? ConvertToJsonNode(item, itemSchema)
                : UnknownValueConverter.ToJsonNode(item));
        }

        return new JsonArray([.. items]);
    }

    JsonNode? ConvertFormattedValueToJsonNode(object? value, JsonSchema schemaProperty)
    {
        // A complex formatted value (e.g. a geospatial type) serializes to a JSON object/array
        // through its registered converter (GeoJSON); a scalar formatted value (guid, date, ...)
        // serializes to a JSON value and goes through the scalar conversion path below.
        var node = JsonSerializer.SerializeToNode(value, SerializerOptions);
        return node is JsonObject or JsonArray ? node : ConvertToJsonValueBasedOnSchemaType(value, schemaProperty);
    }

    JsonValue? ConvertToJsonValueBasedOnSchemaType(object? input, JsonSchema schemaProperty)
    {
        if (input is null)
        {
            return null;
        }

        var targetType = typeFormats.GetTypeForFormat(ConverterSchemas.FormatOf(schemaProperty));
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (input is DateTimeOffset dateTimeOffset && underlyingType == typeof(DateTime))
        {
            return DateTimeValues.ToJsonValue(dateTimeOffset);
        }

        input = underlyingType == typeof(decimal) ? ConvertToDecimal(targetType, input) : TypeConversion.Convert(targetType, input);
        return input.ToJsonValue();
    }
}
