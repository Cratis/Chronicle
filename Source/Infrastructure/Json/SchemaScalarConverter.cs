// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Types;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Converts scalar values between CLR and JSON guided by the type of a schema, for schemas without a known format.
/// </summary>
static class SchemaScalarConverter
{
    /// <summary>
    /// Converts a CLR value to a <see cref="JsonNode"/> according to the type of the schema.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="schemaProperty">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="JsonNode"/>, or null when the value is null or does not match what the schema declares.</returns>
    public static JsonNode? ToJsonNode(object? value, JsonSchema schemaProperty)
    {
        if (value is null)
        {
            return null;
        }

        var schema = ConverterSchemas.ResolveNullable(schemaProperty);
        return ConverterSchemas.TypeOf(schema) switch
        {
            JsonObjectType.String => JsonValue.Create<string>(value is string actualString ? actualString : value.ToString()!),
            JsonObjectType.Boolean => JsonValue.Create<bool>(value is bool actualBool ? actualBool : bool.Parse(value.ToString()!)),
            JsonObjectType.Integer => IntegerToJsonNode(value, schema),
            JsonObjectType.Number => NumberToJsonNode(value),

            // A default-only property ({"default": null}) describes no type, so keep the value as it is.
            JsonObjectType.None => UnknownValueConverter.ToJsonNode(value),

            // The schema declares a type the value does not have - for instance a composite key that a
            // storage provider hands back flattened to a string for an object-typed property. Emitting the
            // mismatched value would make the instance unreadable for the client, so leave it out.
            _ => null
        };
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> holding a value to a CLR value according to the type of the schema.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="schemaProperty">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The CLR value.</returns>
    public static object? FromJsonValue(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        var value = jsonNode.AsValue();
        var isCompliant = ConverterSchemas.HasProtectedValueMetadata(schemaProperty);
        var schema = ConverterSchemas.ResolveNullable(schemaProperty);

        return ConverterSchemas.TypeOf(schema) switch
        {
            JsonObjectType.String => StringFromJson(value, schema),
            JsonObjectType.Boolean => BooleanFromJson(value, isCompliant),
            JsonObjectType.Integer => IntegerFromJson(value, schema, isCompliant),
            JsonObjectType.Number => NumberFromJson(value, isCompliant),

            // No type information in the schema (e.g. an any/empty schema generated for types
            // with custom converters). Fall back to extracting the raw CLR value from the node.
            _ => UnknownValueConverter.ToClrType(jsonNode)
        };
    }

    static JsonValue? IntegerToJsonNode(object value, JsonSchema schema)
    {
        var enumeration = ConverterSchemas.EnumerationOf(schema);
        if (enumeration is null)
        {
            return JsonValue.Create<int>(value is int actualInt ? actualInt : int.Parse(value.ToString()!));
        }

        var longValue = Convert.ToInt64(value);
        var enumIndex = enumeration.Enumeration.ToList().FindIndex(v => Convert.ToInt64(v) == longValue);
        return enumIndex >= 0 && enumIndex < enumeration.EnumerationNames.Count
            ? JsonValue.Create<string>(enumeration.EnumerationNames[enumIndex])
            : null;
    }

    static JsonValue NumberToJsonNode(object value) =>
        value is decimal decimalValue
            ? JsonValue.Create(decimalValue)
            : JsonValue.Create<double>(value is double actualDouble ? actualDouble : double.Parse(value.ToString()!))!;

    static object? StringFromJson(JsonValue value, JsonSchema schema)
    {
        // A node created from a Guid holds the Guid itself; one parsed from JSON text holds a string.
        if (!value.TryGetValue<string>(out var valueAsString))
        {
            if (value.TryGetValue<Guid>(out var guidValue))
            {
                return guidValue;
            }

            valueAsString = value.GetValue<string>();
        }

        return schema.Format == "guid" ? Guid.Parse(valueAsString) : valueAsString;
    }

    static object? BooleanFromJson(JsonValue value, bool isCompliant)
    {
        if (isCompliant && value.TryGetValue<string>(out var text) && text is not null)
        {
            return bool.TryParse(text, out var parsed) ? parsed : text;
        }

        return value.GetValue<bool>();
    }

    static object? IntegerFromJson(JsonValue value, JsonSchema schema, bool isCompliant)
    {
        var enumeration = ConverterSchemas.EnumerationOf(schema);
        if (enumeration is not null && value.TryGetValue<string>(out var enumName))
        {
            var enumValue = EnumerationValueFromName(enumeration, enumName, isCompliant);
            if (enumValue is not null)
            {
                return enumValue;
            }
        }

        if (isCompliant && value.TryGetValue<string>(out var text) && text is not null)
        {
            return ParseIntegerOrKeep(text);
        }

        return value.GetValue<int>();
    }

    static object? EnumerationValueFromName(JsonSchema enumeration, string enumName, bool isCompliant)
    {
        var index = enumeration.EnumerationNames.IndexOf(enumName);
        if (index >= 0 && index < enumeration.Enumeration.Count)
        {
            return TypeConversion.Convert(typeof(int), enumeration.Enumeration.ToArray()[index]);
        }

        return isCompliant ? ParseIntegerOrKeep(enumName) : null;
    }

    static object ParseIntegerOrKeep(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : text;

    static object? NumberFromJson(JsonValue value, bool isCompliant)
    {
        if (isCompliant && value.TryGetValue<string>(out var text) && text is not null)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : text;
        }

        if (value.TryGetValue<decimal>(out var decimalValue) && value.GetValue<object>() is decimal)
        {
            return decimalValue;
        }

        return value.GetValue<double>();
    }
}
