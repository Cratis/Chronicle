// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using NJsonSchema;

namespace Aksio.Cratis.Json;

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
        foreach (var keyValue in expandoObject as IDictionary<string, object?>)
        {
            JsonNode? value = null;

            var name = keyValue.Key;
            if (!schema.ActualProperties.ContainsKey(name))
            {
                ConvertUnknownSchemaTypeToJsonValue(keyValue.Value);
            }
            else
            {
                var schemaProperty = schema.ActualProperties[name];
                value = ConvertToJsonNode(keyValue.Value, schemaProperty);
            }

            jsonObject[name] = value;
        }

        return jsonObject;
    }

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;

        foreach (var (name, sourceValue) in document)
        {
            object? value = null;
            if (sourceValue is not null)
            {
                if (!schema.ActualProperties.ContainsKey(name))
                {
                    value = ConvertUnknownSchemaTypeToClrType(sourceValue!);
                }
                else
                {
                    var schemaProperty = schema.ActualProperties[name];
                    value = ConvertFromJsonNode(sourceValue!, schemaProperty);
                }
            }
            expandoObjectAsDictionary[name] = value!;
        }

        return expandoObject;
    }

    JsonNode? ConvertToJsonNode(object? value, JsonSchemaProperty schemaProperty)
    {
        if (value is ExpandoObject expando)
        {
            return ToJsonObject(
                expando,
                schemaProperty.IsArray ? schemaProperty.Item.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        JsonNode? result;
        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            result = ConvertToJsonValueBasedOnSchemaType(value, schemaProperty);
        }
        else
        {
            result = ConvertToJsonNodeFromUnknownFormat(value, schemaProperty);
        }

        if (result == null && value is IEnumerable enumerable)
        {
            var items = new List<JsonNode?>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertToJsonNode(item, schemaProperty));
            }
            return new JsonArray(items.ToArray());
        }
        return result;
    }

    object? ConvertFromJsonNode(JsonNode jsonNode, JsonSchemaProperty schemaProperty)
    {
        if (jsonNode is JsonObject childObject)
        {
            return ToExpandoObject(
                childObject,
                schemaProperty.IsArray ? schemaProperty.Item.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (jsonNode is JsonArray array)
        {
            return array.Select(_ => ConvertFromJsonNode(_!, schemaProperty)).ToArray();
        }

        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            return ConvertJsonValueToSchemaType(jsonNode, schemaProperty);
        }
        return ConvertJsonValueFromUnknownFormat(jsonNode, schemaProperty);
    }

    object? ConvertJsonValueFromUnknownFormat(JsonNode jsonNode, JsonSchemaProperty schemaProperty)
    {
        var value = jsonNode.AsValue();
        switch (schemaProperty.Type)
        {
            case JsonObjectType.String:
                var valueAsString = value.GetValue<string>();
                return schemaProperty.Format == "guid" ?
                        Guid.Parse(valueAsString) :
                        valueAsString;

            case JsonObjectType.Boolean:
                return value.GetValue<bool>();

            case JsonObjectType.Integer:
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

        if (value is IEnumerable enumerable)
        {
            var array = new JsonArray();

            foreach (var item in enumerable)
            {
                array.Add(ConvertUnknownSchemaTypeToJsonValue(item));
            }

            return array;
        }

        return value.ToJsonValue();
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
            return array.Select(_ => ConvertUnknownSchemaTypeToClrType(value)).ToArray();
        }

        var jsonValue = value.AsValue();

        var element = value.GetValue<JsonElement>();
        if (element.TryGetValue(out var result))
        {
            return result;
        }

        return null;
    }

    object? ConvertJsonValueToSchemaType(JsonNode jsonNode, JsonSchemaProperty schemaProperty)
    {
        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
        return jsonNode.AsValue().ToTargetTypeValue(targetType);
    }

    JsonValue? ConvertToJsonValueBasedOnSchemaType(object? input, JsonSchemaProperty schemaProperty)
    {
        if (input is null)
        {
            return null;
        }

        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
        input = TypeConversion.Convert(targetType, input);
        return input.ToJsonValue();
    }

    JsonNode? ConvertToJsonNodeFromUnknownFormat(object? value, JsonSchemaProperty schemaProperty)
    {
        if (value is null)
        {
            return null;
        }

        switch (schemaProperty.Type)
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
