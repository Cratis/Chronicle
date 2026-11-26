// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Json;

namespace Cratis.Chronicle.Json;

public partial class ExpandoObjectConverter
{
    object? ConvertFromJsonNode(JsonNode? jsonNode, JsonSchema schemaProperty) => jsonNode switch
    {
        null => null,
        JsonObject childObject => ConvertObjectFromJson(childObject, schemaProperty),
        JsonArray array => ConvertArrayFromJson(array, schemaProperty),
        _ => typeFormats.IsKnown(ConverterSchemas.FormatOf(schemaProperty))
            ? ConvertFormattedValueFromJson(jsonNode, schemaProperty)
            : SchemaScalarConverter.FromJsonValue(jsonNode, schemaProperty)
    };

    object? ConvertObjectFromJson(JsonObject childObject, JsonSchema schemaProperty)
    {
        if (schemaProperty.IsDictionary)
        {
            return ToDictionary(childObject);
        }

        // A JSON object carrying a known format identifies a complex CLR type (e.g. a geospatial
        // type serialized as GeoJSON). Deserialize the whole object into that type so the
        // ExpandoObject holds the typed value rather than a generic nested structure.
        var format = ConverterSchemas.FormatOf(schemaProperty);
        if (typeFormats.IsKnown(format))
        {
            return childObject.Deserialize(typeFormats.GetTypeForFormat(format), SerializerOptions);
        }

        return ToExpandoObject(childObject, ConverterSchemas.ObjectSchemaOf(schemaProperty));
    }

    object?[] ConvertArrayFromJson(JsonArray array, JsonSchema schemaProperty)
    {
        // When the schema has no item definition (e.g. an any/empty schema generated for
        // types with custom converters), fall back to unknown-type conversion for each element.
        if (schemaProperty.Item is null)
        {
            return array.Select(UnknownValueConverter.ToClrType).ToArray();
        }

        var items = new object?[array.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = ConvertFromJsonNode(array[i], schemaProperty.Item);
        }

        return items;
    }

    object? ConvertFormattedValueFromJson(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        try
        {
            return ConvertJsonValueToSchemaType(jsonNode, schemaProperty);
        }
        catch (Exception ex) when (
            ConverterSchemas.HasProtectedValueMetadata(schemaProperty) &&
            jsonNode.AsValue().TryGetValue<string>(out _) &&
            ex is FormatException or InvalidCastException or InvalidOperationException or OverflowException)
        {
            // An encrypted value (or the empty marker returned after crypto-shredding) deliberately cannot
            // be parsed as the plaintext formatted type. Keep it opaque for the compliance manager.
            return jsonNode.GetValue<string>();
        }
    }

    Dictionary<string, object> ToDictionary(JsonObject childObject)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in childObject)
        {
            dictionary[key] = UnknownValueConverter.ToClrType(value)!;
        }

        return dictionary;
    }

    object? ConvertJsonValueToSchemaType(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        var targetType = typeFormats.GetTypeForFormat(ConverterSchemas.FormatOf(schemaProperty));
        var value = jsonNode.AsValue();
        if ((Nullable.GetUnderlyingType(targetType) ?? targetType) == typeof(decimal) &&
            DecimalValues.TryFromLiteral(value, out var decimalValue))
        {
            return decimalValue;
        }

        var converted = value.ToTargetTypeValue(targetType);
        return converted is DateTime dateTime && value.TryGetValue<string>(out var text)
            ? DateTimeValues.FromText(dateTime, text)
            : converted;
    }
}
