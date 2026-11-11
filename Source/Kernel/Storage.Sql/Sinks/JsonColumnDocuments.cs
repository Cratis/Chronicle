// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Reflection;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Helpers for reading and writing the loosely typed documents held in the JSON columns of a SQL read model table.
/// </summary>
internal static class JsonColumnDocuments
{
    /// <summary>
    /// Deserialize the JSON held in a column into loosely typed values.
    /// </summary>
    /// <param name="json">The JSON held in the column.</param>
    /// <returns>The document, or null when the column is empty.</returns>
    internal static object? DeserializeJsonColumn(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return ConvertJsonElement(doc.RootElement);
    }

    /// <summary>
    /// Convert a <see cref="JsonElement"/> into loosely typed values.
    /// </summary>
    /// <param name="element">The element to convert.</param>
    /// <returns>An <see cref="ExpandoObject"/>, a list, a string, a number, a boolean or null.</returns>
    internal static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ConvertJsonObject(element),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => ConvertJsonNumber(element),
        JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
        _ => null
    };

    /// <summary>
    /// Serialize a value into the JSON held in a column.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="column">The <see cref="ProjectedColumn"/> the JSON is for.</param>
    /// <returns>The JSON.</returns>
    internal static string SerializeJsonColumn(object? value, ProjectedColumn column)
    {
        if (value is null)
        {
            return column.IsArray ? "[]" : "{}";
        }

        var unwrapped = UnwrapForJson(value);
        return JsonSerializer.Serialize(unwrapped, ReadModelDbContext.JsonSerializerOptions);
    }

    /// <summary>
    /// Unwrap concepts, expando objects, dictionaries and enumerables into values that serialize as plain JSON.
    /// </summary>
    /// <param name="value">The value to unwrap.</param>
    /// <returns>The unwrapped value.</returns>
    internal static object? UnwrapForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            return value.GetConceptValue();
        }

        if (value is ExpandoObject expando)
        {
            return UnwrapDictionary(expando);
        }

        // A schema-defined dictionary property (additionalProperties) round-trips as
        // Dictionary<object, object> (not IDictionary<string, object?>), because its keys and values
        // are typed by whatever the schema declares. Falling through to the generic IEnumerable branch
        // below would iterate its KeyValuePair<,> entries as plain elements, serializing the dictionary
        // as a JSON array of { "Key": ..., "Value": ... } objects instead of a JSON object.
        if (value.GetType().IsDictionary() && value is IEnumerable dictionaryEntries)
        {
            return UnwrapKeyValuePairs(dictionaryEntries);
        }

        if (value is string)
        {
            return value;
        }

        return value is IEnumerable enumerable ? UnwrapEnumerable(enumerable) : value;
    }

    /// <summary>
    /// Unwrap a concept into its value.
    /// </summary>
    /// <param name="value">The value to unwrap.</param>
    /// <returns>The value of the concept, or the value itself when it is not a concept.</returns>
    internal static object? UnwrapConceptValue(object? value) => value?.IsConcept() == true ? value.GetConceptValue() : value;

    /// <summary>
    /// Build the path within a column from a path starting at the column.
    /// </summary>
    /// <param name="path">The path starting at the column.</param>
    /// <returns>The path without the column.</returns>
    internal static PropertyPath BuildSubPath(PropertyPath path)
    {
        var segments = path.Segments.Skip(1).ToArray();
        if (segments.Length == 0)
        {
            return new PropertyPath(string.Empty);
        }

        return PropertyPath.CreateFrom(segments);
    }

    static ExpandoObject ConvertJsonObject(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElement(property.Value);
        }

        return expando;
    }

    static object ConvertJsonNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var integer))
        {
            return integer;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }

    static Dictionary<string, object?> UnwrapDictionary(IDictionary<string, object?> source)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kvp in source)
        {
            dict[kvp.Key] = UnwrapForJson(kvp.Value);
        }

        return dict;
    }

    static List<object?> UnwrapEnumerable(IEnumerable enumerable)
    {
        var list = new List<object?>();
        foreach (var item in enumerable)
        {
            list.Add(UnwrapForJson(item));
        }

        return list;
    }

    static Dictionary<string, object?> UnwrapKeyValuePairs(IEnumerable entries)
    {
        var result = new Dictionary<string, object?>();
        foreach (var entry in entries.GetKeyValuePairs())
        {
            result[entry.Key] = UnwrapForJson(entry.Value);
        }

        return result;
    }
}
