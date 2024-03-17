// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Json;

/// <summary>
/// Extension methods for working with <see cref="JsonObject"/>.
/// </summary>
public static class JsonObjectExtensions
{
    /// <summary>
    /// Deep clones a <see cref="JsonObject"/> for a new instance.
    /// </summary>
    /// <param name="json"><see cref="JsonObject"/> to clone.</param>
    /// <returns>A new <see cref="JsonObject"/> instance.</returns>
    public static JsonObject DeepClone(this JsonObject json)
    {
        return (JsonNode.Parse(json.ToJsonString(Globals.JsonSerializerOptions)) as JsonObject)!;
    }

    /// <summary>
    /// Convert a <see cref="JsonObject"/> to <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="json"><see cref="JsonObject"/> to convert.</param>
    /// <param name="valueConverter">Optional callback for possible value conversion.</param>
    /// <returns>Converted <see cref="ExpandoObject"/>.</returns>
    public static ExpandoObject AsExpandoObject(this JsonObject json, Func<object, object>? valueConverter = default)
    {
        var result = new ExpandoObject();
        var resultAsDictionary = result as IDictionary<string, object>;
        foreach (var (property, node) in json)
        {
            resultAsDictionary[property!] = ConvertNode(node!, valueConverter);
        }

        return result;
    }

    static object ConvertNode(JsonNode node, Func<object, object>? valueConverter = default)
    {
        switch (node)
        {
            case JsonValue jsonValue:
                jsonValue.TryGetValue(out object? sourceValue);

                if (sourceValue is JsonElement element)
                {
                    element.TryGetValue(out sourceValue);
                }

                if (valueConverter is not null)
                {
                    sourceValue = valueConverter(sourceValue!);
                }

                return sourceValue!;

            case JsonObject jsonObject:
                return jsonObject.AsExpandoObject();

            case JsonArray jsonArray:
                return jsonArray
                    .Where(_ => _ is not null)
                    .Select(_ => ConvertNode(_!))
                    .ToArray();
        }

        return null!;
    }
}
