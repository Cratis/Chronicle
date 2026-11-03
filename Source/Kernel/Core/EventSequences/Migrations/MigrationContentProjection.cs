// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Projects migration JSON onto a generation's schema without changing scalar representations.
/// </summary>
internal static class MigrationContentProjection
{
    /// <summary>
    /// Creates the content carried forward to the next migration using converted values only as a structural witness.
    /// </summary>
    /// <param name="document">The raw migration content.</param>
    /// <param name="schema">The target generation's schema.</param>
    /// <param name="converted">The content already converted with the target schema.</param>
    /// <returns>An independent JSON document containing the selected content.</returns>
    internal static JsonObject Project(JsonObject document, JsonSchema schema, ExpandoObject converted) =>
        ProjectObject(document, schema, converted);

    static JsonObject ProjectObject(JsonObject document, JsonSchema schema, ExpandoObject converted)
    {
        var properties = schema.GetFlattenedProperties().ToList();
        if (properties.Count == 0)
        {
            return (JsonObject)document.DeepClone();
        }

        var result = new JsonObject();
        var convertedMembers = (IDictionary<string, object?>)converted;
        foreach (var property in properties)
        {
            var selected = SelectProperty(document, property.Name);
            if (selected.Key is not null)
            {
                convertedMembers.TryGetValue(property.Name, out var convertedValue);
                result[selected.Key] = ProjectNode(selected.Value, property, convertedValue);
            }
        }

        return result;
    }

    static KeyValuePair<string, JsonNode?> SelectProperty(JsonObject document, string name)
    {
        // Match conversion's exact non-null lookup, then its first case-insensitive match.
        if (document[name] is { } value)
        {
            return new(name, value);
        }

        return document.FirstOrDefault(property => property.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    static JsonNode? ProjectNode(JsonNode? node, JsonSchema schema, object? converted)
    {
        if (node is JsonObject document)
        {
            // Dictionaries and formatted CLR objects are opaque to schema-based property filtering.
            if (schema.IsDictionary || converted is not ExpandoObject convertedObject)
            {
                return document.DeepClone();
            }

            var objectSchema = schema.IsArray ? schema.Item!.Reference ?? schema.Item : schema.ActualTypeSchema;
            return ProjectObject(document, objectSchema!, convertedObject);
        }

        if (node is JsonArray array && schema.Item is { } itemSchema)
        {
            var convertedItems = (object?[])converted!;
            var result = new JsonArray();
            for (var index = 0; index < array.Count; index++)
            {
                result.Add(ProjectNode(array[index], itemSchema, convertedItems[index]));
            }

            return result;
        }

        // Scalars, explicit nulls and arrays without a direct item schema retain their original JSON.
        return node?.DeepClone();
    }
}
