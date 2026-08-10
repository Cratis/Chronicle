// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Json;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
public class ExpandoObjectConverter(ITypeFormats typeFormats) : IExpandoObjectConverter
{
    /// <inheritdoc/>
    public JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        var jsonObject = new JsonObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;
        var schemaProperties = schema.GetFlattenedProperties().ToList();

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the expando object is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (key, value) in expandoObjectAsDictionary)
            {
                var node = ConvertUnknownSchemaTypeToJsonValue(value);
                if (node is not null)
                {
                    jsonObject[key] = node;
                }
            }

            return jsonObject;
        }

        foreach (var property in schemaProperties)
        {
            var name = property.Name;

            // Prefer an exact-case match; fall back to a case-insensitive match. The source expando
            // may carry both casings of the same name (e.g. both `Id` and `id`).
            if (!expandoObjectAsDictionary.TryGetValue(name, out var sourceValue))
            {
                sourceValue = expandoObjectAsDictionary.FirstOrDefault(_ => _.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
            }

            var value = ConvertToJsonNode(sourceValue, property);

            if (value is null)
            {
                var defaultValue = property.GetDefaultValue(typeFormats);
                if (defaultValue is not null)
                {
                    value = ConvertToJsonNode(defaultValue, property);
                }
            }

            if (value is not null)
            {
                jsonObject[name] = value;
            }
        }

        return jsonObject;
    }

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;

        var schemaProperties = schema.GetFlattenedProperties().ToList();

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the document is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (name, sourceValue) in document)
            {
                if (sourceValue is not null)
                {
                    expandoObjectAsDictionary[name] = ConvertUnknownSchemaTypeToClrType(sourceValue);
                }
            }

            return expandoObject;
        }

        foreach (var property in schemaProperties)
        {
            var name = property.Name;
            var sourceValue = document[name]
                ?? document.FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

            object? value = null;
            if (sourceValue is not null)
            {
                value = ConvertFromJsonNode(sourceValue, property);
            }

            value ??= property.GetDefaultValue(typeFormats);
            if (value is not null)
            {
                expandoObjectAsDictionary[name] = value;
            }
        }

        return expandoObject;
    }

    JsonNode? ConvertToJsonNode(object? value, JsonSchema schemaProperty)
    {
        // Compliance handlers replace protected scalar values with opaque strings while the registered schema
        // intentionally remains the schema of the plaintext event. Keep those strings opaque until the
        // compliance manager releases them instead of coercing ciphertext through the plaintext scalar type.
        if (value is string compliantValue && schemaProperty.GetComplianceMetadata().Any())
        {
            return JsonValue.Create(compliantValue);
        }

        if (schemaProperty.IsDictionary)
        {
            return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        if (value is ExpandoObject expando)
        {
            return ToJsonObject(
                expando,
                schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        // A coarse [PII] value on a whole list/array is blob-encrypted to a single ciphertext string,
        // even though the schema type stays Array. Serialize it as that scalar string rather than letting
        // the array branch below treat the string as an enumerable of characters — which would shred the
        // ciphertext into per-character elements and break the encrypt -> store -> read -> release round trip.
        if (value is string ciphertext && schemaProperty.Type.HasFlag(JsonObjectType.Array))
        {
            return JsonValue.Create(ciphertext);
        }

        if (schemaProperty.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable enumerable)
        {
            var items = new List<JsonNode?>();
            var itemSchema = schemaProperty.Item?.Reference ?? schemaProperty.Item;
            foreach (var item in enumerable)
            {
                items.Add(itemSchema is not null
                    ? ConvertToJsonNode(item, itemSchema)
                    : ConvertUnknownSchemaTypeToJsonValue(item));
            }
            return new JsonArray([.. items]);
        }

        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            // A complex formatted value (e.g. a geospatial type) serializes to a JSON object/array
            // through its registered converter (GeoJSON); a scalar formatted value (guid, date, ...)
            // serializes to a JSON value and goes through the scalar conversion path below.
            var node = JsonSerializer.SerializeToNode(value, Globals.JsonSerializerOptions);
            if (node is JsonObject or JsonArray)
            {
                return node;
            }

            return ConvertToJsonValueBasedOnSchemaType(value, schemaProperty);
        }

        // Fall back to schema-agnostic conversion for collection types that didn't match
        // the schema-typed array branch above (e.g. when NJsonSchema doesn't set Type=Array).
        if (value is IEnumerable and not string)
        {
            return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        return ConvertToJsonNodeFromUnknownFormat(value, schemaProperty);
    }

    object? ConvertFromJsonNode(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        if (jsonNode is JsonObject childObject)
        {
            if (schemaProperty.IsDictionary)
            {
                return ToDictionary(childObject);
            }

            // A JSON object carrying a known format identifies a complex CLR type (e.g. a geospatial
            // type serialized as GeoJSON). Deserialize the whole object into that type so the
            // ExpandoObject holds the typed value rather than a generic nested structure.
            if (typeFormats.IsKnown(schemaProperty.Format!))
            {
                return jsonNode.Deserialize(
                    typeFormats.GetTypeForFormat(schemaProperty.Format!),
                    Globals.JsonSerializerOptions);
            }

            return ToExpandoObject(
                childObject,
                schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (jsonNode is JsonArray array)
        {
            // When the schema has no item definition (e.g. an any/empty schema generated for
            // types with custom converters), fall back to unknown-type conversion for each element.
            if (schemaProperty.Item is null)
            {
                return array.Select(_ => ConvertUnknownSchemaTypeToClrType(_!)).ToArray();
            }
            return array.Select(_ => ConvertFromJsonNode(_!, schemaProperty.Item!)).ToArray();
        }

        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            try
            {
                return ConvertJsonValueToSchemaType(jsonNode, schemaProperty);
            }
            catch (Exception ex) when (
                schemaProperty.GetComplianceMetadata().Any() &&
                jsonNode.AsValue().TryGetValue<string>(out _) &&
                ex is FormatException or InvalidCastException or InvalidOperationException or OverflowException)
            {
                // An encrypted value (or the empty marker returned after crypto-shredding) deliberately cannot
                // be parsed as the plaintext formatted type. Keep it opaque for the compliance manager.
                return jsonNode.GetValue<string>();
            }
        }
        return ConvertJsonValueFromUnknownFormat(jsonNode, schemaProperty);
    }

    Dictionary<object, object> ToDictionary(JsonObject childObject)
    {
        var dictionary = new Dictionary<object, object>();
        foreach (var (key, value) in childObject)
        {
            dictionary[key] = ConvertUnknownSchemaTypeToClrType(value!)!;
        }

        return dictionary;
    }

    object? ConvertJsonValueFromUnknownFormat(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        if (jsonNode is null)
        {
            return null;
        }

        var value = jsonNode.AsValue();

        var isCompliant = schemaProperty.GetComplianceMetadata().Any();

        // Nullable enum values are represented as a discriminated union with a null value. We need to get the actual property definition.
        // Other types could also be represented in this manner and it is therefor important to get the actual property definition.
        if (schemaProperty.OneOf.Count > 0)
        {
            var oneOfSchema = schemaProperty.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
            if (oneOfSchema != default)
            {
                schemaProperty = oneOfSchema;
            }
        }
        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                schemaProperty.Reference!.Type :
                schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        var genericArguments = value.GetType().GetGenericArguments();

        switch (type)
        {
            case JsonObjectType.String:
                var genericArgs = value.GetType().GetGenericArguments();
                if (genericArgs.Length == 1 && genericArgs[0] == typeof(Guid))
                {
                    return value.GetValue<Guid>();
                }
                var valueAsString = value.GetValue<string>();
                return schemaProperty.Format == "guid" ?
                        Guid.Parse(valueAsString) :
                        valueAsString;

            case JsonObjectType.Boolean:
                if (isCompliant && value.TryGetValue<string>(out var booleanAsString))
                {
                    if (bool.TryParse(booleanAsString, out var booleanValue))
                    {
                        return booleanValue;
                    }
                    if (booleanAsString is not null)
                    {
                        return booleanAsString;
                    }
                }
                return value.GetValue<bool>();

            case JsonObjectType.Integer:
                JsonSchema? enumReadSchema = null;
                if (schemaProperty.Reference?.IsEnumeration == true)
                    enumReadSchema = schemaProperty.Reference;
                else if (schemaProperty.IsEnumeration)
                    enumReadSchema = schemaProperty;
                if (enumReadSchema is not null && value.TryGetValue<string>(out var enumNameValue))
                {
                    var index = enumReadSchema.EnumerationNames.IndexOf(enumNameValue);
                    if (index >= 0 && index < enumReadSchema.Enumeration.Count)
                        return TypeConversion.Convert(typeof(int), enumReadSchema.Enumeration.ToArray()[index]!);

                    if (isCompliant)
                    {
                        if (int.TryParse(enumNameValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumValue))
                            return enumValue;

                        return enumNameValue;
                    }
                }
                if (isCompliant && value.TryGetValue<string>(out var integerAsString))
                {
                    if (int.TryParse(integerAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
                    {
                        return integerValue;
                    }
                    if (integerAsString is not null)
                    {
                        return integerAsString;
                    }
                }
                return value.GetValue<int>();

            case JsonObjectType.Number:
                if (isCompliant && value.TryGetValue<string>(out var numberAsString))
                {
                    if (double.TryParse(numberAsString, NumberStyles.Float, CultureInfo.InvariantCulture, out var numberValue))
                    {
                        return numberValue;
                    }
                    if (numberAsString is not null)
                    {
                        return numberAsString;
                    }
                }
                return value.GetValue<double>();
        }

        // No type information in the schema (e.g. an any/empty schema generated for types
        // with custom converters). Fall back to extracting the raw CLR value from the node.
        return ConvertUnknownSchemaTypeToClrType(jsonNode);
    }

    JsonNode? ConvertUnknownSchemaTypeToJsonValue(object? value)
    {
        if (value is ExpandoObject expandoObject)
        {
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            var document = new JsonObject();

            foreach (var kvp in expandoObjectAsDictionary)
            {
                document[kvp.Key] = ConvertUnknownSchemaTypeToJsonValue(kvp.Value);
            }
            return document;
        }

        if (value?.GetType().IsDictionary() == true)
        {
            var dictionaryType = value.GetType();
            var keyType = dictionaryType.GetKeyType();
            var valueType = dictionaryType.GetValueType();
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
            var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;

            var dictionary = value as IEnumerable;
            var document = new JsonObject();
            foreach (var keyValuePair in dictionary!)
            {
                var key = keyProperty.GetValue(keyValuePair)?.ToString() ?? string.Empty;
                document[key] = ConvertUnknownSchemaTypeToJsonValue(valueProperty.GetValue(keyValuePair));
            }
            return document;
        }

        if (value?.GetType().IsEnum == true)
        {
            return JsonValue.Create(Convert.ToInt32(value));
        }

        var jsonValue = value.ToJsonValue();
        if (jsonValue is not null)
        {
            return jsonValue;
        }

        if (value is IEnumerable enumerable)
        {
            var array = new JsonArray();

            foreach (var item in enumerable)
            {
                array.Add(ConvertUnknownSchemaTypeToJsonValue(item));
            }

            return array;
        }

        return null;
    }

    object? ConvertUnknownSchemaTypeToClrType(JsonNode value)
    {
        if (value is JsonObject jsonObject)
        {
            var expandoObject = new ExpandoObject();
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            foreach (var (property, sourceValue) in jsonObject)
            {
                expandoObjectAsDictionary[property] = ConvertUnknownSchemaTypeToClrType(sourceValue!)!;
            }
            return expandoObject;
        }

        if (value is JsonArray array)
        {
            return array.Select(_ => ConvertUnknownSchemaTypeToClrType(_!)).ToArray();
        }

        var jsonValue = value.GetValue<object>();
        if (jsonValue is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };
        }

        return jsonValue;
    }

    object? ConvertJsonValueToSchemaType(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format!);
        return jsonNode.AsValue().ToTargetTypeValue(targetType);
    }

    JsonValue? ConvertToJsonValueBasedOnSchemaType(object? input, JsonSchema schemaProperty)
    {
        if (input is null)
        {
            return null;
        }

        var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format!);
        input = TypeConversion.Convert(targetType, input);
        return input.ToJsonValue();
    }

    JsonValue? ConvertToJsonNodeFromUnknownFormat(object? value, JsonSchema schemaProperty)
    {
        if (value is null)
        {
            return null;
        }

        // Nullable enum values are represented as a discriminated union with a null value. We need to get the actual property definition.
        // Other types could also be represented in this manner and it is therefor important to get the actual property definition.
        if (schemaProperty.OneOf.Count > 0)
        {
            var oneOfSchema = schemaProperty.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
            if (oneOfSchema != default)
            {
                schemaProperty = oneOfSchema;
            }
        }
        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                schemaProperty.Reference!.Type :
                schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        switch (type)
        {
            case JsonObjectType.String:
                return JsonValue.Create<string>(value is string actualString ? actualString : value.ToString()!);

            case JsonObjectType.Boolean:
                return JsonValue.Create<bool>(value is bool actualBool ? actualBool : bool.Parse(value.ToString()!));

            case JsonObjectType.Integer:
                JsonSchema? enumRef = null;
                if (schemaProperty.Reference?.IsEnumeration == true)
                    enumRef = schemaProperty.Reference;
                else if (schemaProperty.IsEnumeration)
                    enumRef = schemaProperty;
                if (enumRef is not null)
                {
                    var enumValues = enumRef.Enumeration.ToList();
                    var longValue = Convert.ToInt64(value);
                    var enumIndex = enumValues.FindIndex(v => Convert.ToInt64(v) == longValue);
                    if (enumIndex >= 0 && enumIndex < enumRef.EnumerationNames.Count)
                        return JsonValue.Create<string>(enumRef.EnumerationNames[enumIndex]);
                    return null;
                }
                return JsonValue.Create<int>(value is int actualInt ? actualInt : int.Parse(value.ToString()!));

            case JsonObjectType.Number:
                return JsonValue.Create<double>(value is double actualDouble ? actualDouble : double.Parse(value.ToString()!));
        }

        return null;
    }
}
