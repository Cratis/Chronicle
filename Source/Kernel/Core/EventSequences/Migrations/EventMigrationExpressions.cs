// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Evaluates the built-in expressions an event type migration carries for transformations JMESPath cannot express
/// on its own.
/// </summary>
internal static class EventMigrationExpressions
{
    /// <summary>
    /// Extracts one part of a source property's value, split on a separator.
    /// </summary>
    /// <param name="content">The event content being migrated.</param>
    /// <param name="config">The configuration of the expression.</param>
    /// <returns>The extracted part, or an empty value when there is nothing to extract.</returns>
    public static JsonValue? EvaluateSplit(JsonObject content, JsonObject config)
    {
        var source = config["source"]?.GetValue<string>();
        var separator = config["separator"]?.GetValue<string>() ?? string.Empty;
        var part = config["part"]?.GetValue<int>() ?? 0;

        if (source is null || !content.TryGetPropertyValue(source, out var sourceNode) || sourceNode is null)
            return JsonValue.Create(string.Empty);

        var sourceValue = sourceNode.GetValue<string>();
        var parts = sourceValue.Split(separator);
        return JsonValue.Create(parts.Length > part ? parts[part] : string.Empty);
    }

    /// <summary>
    /// Concatenates several source properties into one value.
    /// </summary>
    /// <param name="content">The event content being migrated.</param>
    /// <param name="config">The configuration of the expression.</param>
    /// <returns>The concatenated value.</returns>
    public static JsonValue? EvaluateCombine(JsonObject content, JsonObject config)
    {
        if (config["sources"] is not JsonArray sources)
            return JsonValue.Create(string.Empty);

        var separator = config["separator"]?.GetValue<string>() ?? string.Empty;

        var builder = new StringBuilder();
        var first = true;
        foreach (var source in sources)
        {
            var propertyName = source?.GetValue<string>();
            if (propertyName != null && content.TryGetPropertyValue(propertyName, out var node) && node != null)
            {
                if (!first)
                    builder.Append(separator);
                builder.Append(node.GetValue<string>());
                first = false;
            }
        }
        return JsonValue.Create(builder.ToString());
    }

    /// <summary>
    /// Reads the value a property carried under its previous name.
    /// </summary>
    /// <param name="content">The event content being migrated.</param>
    /// <param name="oldName">The name the property had in the source generation.</param>
    /// <returns>The value, or <see langword="null"/> when the source generation did not carry it.</returns>
    public static JsonNode? EvaluateRename(JsonObject content, string? oldName)
    {
        if (oldName is null || !content.TryGetPropertyValue(oldName, out var value))
            return null;
        return value?.DeepClone();
    }

    /// <summary>
    /// Translates a source property's value through a value map.
    /// </summary>
    /// <param name="content">The event content being migrated.</param>
    /// <param name="config">The configuration of the expression.</param>
    /// <param name="value">When this returns <see langword="true"/>, the translated value.</param>
    /// <returns><see langword="true"/> when the expression produced a value; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A value the map does not mention is carried across unchanged - the map states what changed meaning between
    /// two generations, so every value it stays silent about means in the target generation exactly what it meant in
    /// the source one. When the source property is absent from the payload altogether there is nothing to translate,
    /// and this reports no result rather than writing a null over whatever the surrounding transformation produced.
    /// </remarks>
    public static bool TryEvaluateMapValues(JsonObject content, JsonObject config, out JsonNode? value)
    {
        value = null;

        var source = config["source"]?.GetValue<string>();
        if (source is null || !content.TryGetPropertyValue(source, out var sourceNode))
        {
            return false;
        }

        value = sourceNode?.DeepClone();

        if (config["mappings"] is not JsonArray mappings)
        {
            return true;
        }

        var sourceValue = JsonValues.Canonical(sourceNode);
        var mapped = mappings
            .OfType<JsonObject>()
            .FirstOrDefault(mapping => JsonValues.Canonical(mapping["from"]) == sourceValue);

        if (mapped is not null)
        {
            value = mapped["to"]?.DeepClone();
        }

        return true;
    }
}
