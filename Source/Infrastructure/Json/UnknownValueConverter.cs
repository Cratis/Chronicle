// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Json;
using Cratis.Reflection;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Converts values between CLR and JSON without a schema to guide it.
/// </summary>
static class UnknownValueConverter
{
    /// <summary>
    /// Converts a CLR value to a <see cref="JsonNode"/> by inspecting the value itself.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The <see cref="JsonNode"/>, or null when the value has no JSON representation.</returns>
    public static JsonNode? ToJsonNode(object? value)
    {
        if (value is ExpandoObject expandoObject)
        {
            return ExpandoToJsonObject(expandoObject);
        }

        if (value is string text)
        {
            return JsonValue.Create(text);
        }

        if (value is IDictionary dictionaryValue)
        {
            return DictionaryToJsonObject(dictionaryValue);
        }

        if (value?.GetType() is { IsValueType: false } valueType && valueType.IsDictionary())
        {
            return GenericDictionaryToJsonObject(value, valueType);
        }

        if (value?.GetType().IsEnum == true)
        {
            return JsonValue.Create(Convert.ToInt32(value));
        }

        return value.ToJsonValue() ?? (value is IEnumerable enumerable ? (JsonNode)EnumerableToJsonArray(enumerable) : null);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a CLR value by inspecting the node itself.
    /// </summary>
    /// <param name="value">The <see cref="JsonNode"/> to convert.</param>
    /// <returns>The CLR value.</returns>
    public static object? ToClrType(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonObject jsonObject)
        {
            return JsonObjectToExpando(jsonObject);
        }

        if (value is JsonArray array)
        {
            return array.Select(ToClrType).ToArray();
        }

        var jsonValue = value.GetValue<object>();
        return jsonValue is JsonElement element ? ElementToClrType(element) : jsonValue;
    }

    static JsonObject ExpandoToJsonObject(ExpandoObject expandoObject)
    {
        var document = new JsonObject();
        foreach (var kvp in (IDictionary<string, object?>)expandoObject)
        {
            document[kvp.Key] = ToJsonNode(kvp.Value);
        }

        return document;
    }

    static JsonObject DictionaryToJsonObject(IDictionary dictionaryValue)
    {
        var document = new JsonObject();
        var entries = dictionaryValue.GetEnumerator();
        while (entries.MoveNext())
        {
            document[entries.Key.ToString() ?? string.Empty] = ToJsonNode(entries.Value);
        }

        return document;
    }

    static JsonObject GenericDictionaryToJsonObject(object value, Type dictionaryType)
    {
        var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(dictionaryType.GetKeyType(), dictionaryType.GetValueType());
        var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
        var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;

        var document = new JsonObject();
        foreach (var keyValuePair in (IEnumerable)value)
        {
            var key = keyProperty.GetValue(keyValuePair)?.ToString() ?? string.Empty;
            document[key] = ToJsonNode(valueProperty.GetValue(keyValuePair));
        }

        return document;
    }

    static JsonArray EnumerableToJsonArray(IEnumerable enumerable)
    {
        var array = new JsonArray();
        foreach (var item in enumerable)
        {
            array.Add(ToJsonNode(item));
        }

        return array;
    }

    static ExpandoObject JsonObjectToExpando(JsonObject jsonObject)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
        foreach (var (property, sourceValue) in jsonObject)
        {
            expandoObjectAsDictionary[property] = ToClrType(sourceValue)!;
        }

        return expandoObject;
    }

    static object? ElementToClrType(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
