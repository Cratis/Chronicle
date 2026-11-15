// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Projections.Engine.Graphs.Views;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Converts what a graph holds to JSON for a preview.
/// </summary>
internal static class GraphPreviewJson
{
    /// <summary>
    /// Convert the properties of a node or an edge to a JSON object.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <returns>The <see cref="JsonObject"/>.</returns>
    internal static JsonObject ToJson(this IReadOnlyDictionary<string, object?> properties) =>
        new(properties.Select(_ => KeyValuePair.Create(_.Key, ToNode(_.Value))));

    /// <summary>
    /// Convert a row of a view to a JSON object, with the nested rows of its relations.
    /// </summary>
    /// <param name="row">The <see cref="GraphViewRow"/>.</param>
    /// <returns>The <see cref="JsonObject"/>.</returns>
    internal static JsonObject ToJson(this GraphViewRow row) => row.Values.ToJson();

    static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        GraphViewRow row => row.ToJson(),
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        DateTimeOffset moment => JsonValue.Create(moment.ToString("O", CultureInfo.InvariantCulture)),
        IEnumerable items => new JsonArray([.. items.Cast<object?>().Select(ToNode)]),
        _ => ToScalar(value)
    };

    static JsonValue? ToScalar(object value) => GraphValues.Normalize(value) switch
    {
        long number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        decimal number => JsonValue.Create(number),
        var other => JsonValue.Create(GraphValues.TextOf(other))
    };
}
