// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Json;
using Aksio.Types;
using Cratis.Reflection;
using Cratis.Schemas;
using NJsonSchema;

namespace Cratis.Json;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
public class ExpandoObjectConverter : IExpandoObjectConverter
{
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
    public ExpandoObjectConverter(ITypeFormats typeFormats) =>
        _typeFormats = typeFormats;

    /// <inheritdoc/>
    public JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        var jsonObject = new JsonObject();
        var schemaProperties = schema.GetFlattenedProperties();
        foreach (var property in schemaProperties)
        {
            JsonNode? value = null;

            var keyValue = expandoObject!.SingleOrDefault(_ => _.Key == property.Name);

            var name = property.Name;
            var schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == name);
            if (schemaProperty is null)
            {
                ConvertUnknownSchemaTypeToJsonValue(keyValue.Value);
            }
            else
            {
                value = ConvertToJsonNode(keyValue.Value, schemaProperty);
            }

            if (value is null)
            {
                var defaultValue = property.GetDefaultValue(_typeFormats);
                if (defaultValue is not null)
                {
                    value = defaultValue.ToJsonValue();
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

        var schemaProperties = schema.GetFlattenedProperties();
        foreach (var property in schemaProperties)
        {
            var name = property.Name;
            var sourceValue = document[name];

            object? value = null;
            if (sourceValue is not null)
            {
                var schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == name);
                if (schemaProperty is null)
                {
                    value = ConvertUnknownSchemaTypeToClrType(sourceValue!);
                }
                else
                {
                    value = ConvertFromJsonNode(sourceValue!, schemaProperty);
                }
            }

            value ??= property.GetDefaultValue(_typeFormats);
            expandoObjectAsDictionary[name] = value;
        }

        return expandoObject;
    }

    JsonNode? ConvertToJsonNode(object? value, JsonSchema schemaProperty)
    {
        if (schemaProperty.IsDictionary)
        {
            return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        if (value is ExpandoObject expando)
        {
            return ToJsonObject(
                expando,
                schemaProperty.IsArray ? schemaProperty.Item.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (schemaProperty.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable enumerable)
        {
            var items = new List<JsonNode?>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertToJsonNode(item, schemaProperty.Item.Reference ?? schemaProperty.Item));
            }
            return new JsonArray(items.ToArray());
        }

        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            return ConvertToJsonValueBasedOnSchemaType(value, schemaProperty);
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

            return ToExpandoObject(
                childObject,
                schemaProperty.IsArray ? schemaProperty.Item.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (jsonNode is JsonArray array)
        {
            return array.Select(_ => ConvertFromJsonNode(_!, schemaProperty.Item)).ToArray();
        }

        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            return ConvertJsonValueToSchemaType(jsonNode, schemaProperty);
        }
        return ConvertJsonValueFromUnknownFormat(jsonNode, schemaProperty);
    }

    IDictionary<object, object> ToDictionary(JsonObject childObject)
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
                schemaProperty.Reference.Type :
                schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        var genericArguments = value.GetType().GetGenericArguments();

        switch (type)
        {
            case JsonObjectType.String:
                var valueAsString = value.GetValue<string>();
                return schemaProperty.Format == "guid" ?
                        Guid.Parse(valueAsString) :
                        valueAsString;

            case JsonObjectType.Boolean:
                return value.GetValue<bool>();

            case JsonObjectType.Integer:
                if (genericArguments.Length == 1 &&
                    genericArguments[0] == typeof(string) &&
                    schemaProperty.Reference?.IsEnumeration == true)
                {
                    var index = schemaProperty.Reference.EnumerationNames.IndexOf(value.GetValue<string>());
                    return TypeConversion.Convert(typeof(int), schemaProperty.Reference.Enumeration.ToArray()[index]);
                }
                return value.GetValue<int>();

            case JsonObjectType.Number:
                return value.GetValue<double>();
        }

        return null!;
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
        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
        return jsonNode.AsValue().ToTargetTypeValue(targetType);
    }

    JsonValue? ConvertToJsonValueBasedOnSchemaType(object? input, JsonSchema schemaProperty)
    {
        if (input is null)
        {
            return null;
        }

        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
        input = TypeConversion.Convert(targetType, input);
        return input.ToJsonValue();
    }

    JsonNode? ConvertToJsonNodeFromUnknownFormat(object? value, JsonSchema schemaProperty)
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
                schemaProperty.Reference.Type :
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
                return JsonValue.Create<int>(value is int actualInt ? actualInt : int.Parse(value.ToString()!));

            case JsonObjectType.Number:
                return JsonValue.Create<double>(value is double actualDouble ? actualDouble : double.Parse(value.ToString()!));
        }

        return null;
    }
}
