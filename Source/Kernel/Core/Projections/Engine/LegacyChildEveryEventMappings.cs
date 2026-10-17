// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Normalizes unambiguous child every-event mappings emitted at the root by older clients.
/// </summary>
internal static partial class LegacyChildEveryEventMappings
{
    /// <summary>
    /// Moves bare root mappings missing from the root schema to each descendant that declares the property.
    /// </summary>
    /// <param name="definition">The registered projection definition, which is left unchanged.</param>
    /// <param name="schema">The root read-model schema.</param>
    /// <returns>The normalized definition.</returns>
    internal static ProjectionDefinition Normalize(ProjectionDefinition definition, JsonSchema schema)
    {
        if (!definition.FromEvery.IncludeChildren)
        {
            return definition;
        }

        var candidates = definition.FromEvery.Properties
            .Where(mapping => mapping.Key.Segments.Count() == 1 &&
                              mapping.Key.LastSegment is PropertyName &&
                              FindProperty(schema, mapping.Key.Path) is null)
            .ToDictionary();
        if (candidates.Count == 0)
        {
            return definition;
        }

        var moved = new HashSet<PropertyPath>();
        var children = NormalizeDescendants(definition.Children, schema, candidates, moved, isCollection: true);
        var nested = NormalizeDescendants(definition.Nested, schema, candidates, moved, isCollection: false);

        return definition with
        {
            Children = children!,
            Nested = nested,
            FromEvery = definition.FromEvery with
            {
                Properties = definition.FromEvery.Properties.Where(mapping => !moved.Contains(mapping.Key)).ToDictionary()
            }
        };
    }

    [LoggerMessage(LogLevel.Debug, "Projection '{ProjectionId}' moved legacy root every-event mapping '{Property}' to matching descendants.")]
    internal static partial void LogMoved(ILogger<ProjectionFactory> logger, string projectionId, string property);

    static Dictionary<PropertyPath, ChildrenDefinition>? NormalizeDescendants(
        IDictionary<PropertyPath, ChildrenDefinition>? definitions,
        JsonSchema schema,
        IDictionary<PropertyPath, string> candidates,
        ISet<PropertyPath> moved,
        bool isCollection) => definitions?.ToDictionary(
            entry => entry.Key,
            entry =>
            {
                var property = FindProperty(schema, entry.Key.LastSegment.Value);
                var descendantSchema = isCollection ? property?.Item?.ActualSchema : property?.ActualSchema;
                return descendantSchema is null ? entry.Value : NormalizeChild(entry.Value, descendantSchema, candidates, moved);
            });

    static ChildrenDefinition NormalizeChild(
        ChildrenDefinition definition,
        JsonSchema schema,
        IDictionary<PropertyPath, string> candidates,
        ISet<PropertyPath> moved)
    {
        var properties = definition.FromEvery.Properties.ToDictionary();
        var matches = candidates.Where(mapping => FindProperty(schema, mapping.Key.Path) is not null).ToArray();
        foreach (var mapping in matches)
        {
            // An explicit child-level mapping is authoritative when both forms were registered.
            properties.TryAdd(mapping.Key, mapping.Value);
            moved.Add(mapping.Key);
        }

        var all = definition.FromEvery with
        {
            Properties = properties,
            IncludeChildren = definition.FromEvery.IncludeChildren || matches.Length > 0
        };

        // ChildrenDefinition exposes All as well as the inherited FromEvery; keep both views consistent.
        return definition with
        {
            All = all,
            FromEvery = all,
            Children = NormalizeDescendants(definition.Children, schema, candidates, moved, isCollection: true)!,
            Nested = NormalizeDescendants(definition.Nested, schema, candidates, moved, isCollection: false)
        };
    }

    static JsonSchemaProperty? FindProperty(JsonSchema schema, string name) =>
        schema.Properties.TryGetValue(name, out var property)
            ? property
            : schema.Properties.Values.FirstOrDefault(candidate => candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
