// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Extracts the source properties a migration expression reads from.
/// </summary>
static class MigrationExpressionSources
{
    /// <summary>
    /// Gets the source property paths a migration expression value reads from.
    /// </summary>
    /// <param name="value">The expression value from the upcast or downcast JMESPath object.</param>
    /// <returns>The source property paths, lazily evaluated.</returns>
    internal static IEnumerable<string> From(JsonNode? value) => value switch
    {
        JsonValue jsonValue when jsonValue.TryGetValue<string>(out var path) => FromPath(path),
        JsonObject { Count: 1 } expression => FromExpression(expression.GetAt(0)),
        _ => []
    };

    /// <summary>
    /// A JmesPath expression like "@.propertyName" reads the property name that follows the prefix.
    /// </summary>
    /// <param name="path">The string value of the expression.</param>
    /// <returns>The property name, or nothing when the value is not such an expression.</returns>
    static IEnumerable<string> FromPath(string path) => path.StartsWith("@.") ? [path[2..]] : [];

    static IEnumerable<string> FromExpression(KeyValuePair<string, JsonNode?> entry) => entry switch
    {
        { Key: WellKnownExpressions.Rename, Value: JsonValue oldName } when oldName.TryGetValue<string>(out var name) => [name],
        { Key: WellKnownExpressions.Split, Value: JsonObject config } => SourceOf(config),
        { Key: WellKnownExpressions.Combine, Value: JsonObject config } => CombinedSources(config),
        { Key: WellKnownExpressions.MapValues, Value: JsonObject config } => SourceOf(config),
        _ => []
    };

    static IEnumerable<string> SourceOf(JsonObject config)
    {
        var source = config["source"]?.GetValue<string>();
        if (source is not null)
        {
            yield return source;
        }
    }

    static IEnumerable<string> CombinedSources(JsonObject config)
    {
        if (config["sources"] is not JsonArray sources)
        {
            yield break;
        }

        foreach (var item in sources)
        {
            var name = item?.GetValue<string>();
            if (name is not null)
            {
                yield return name;
            }
        }
    }
}
