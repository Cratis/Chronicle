// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// A schema-resolved property in an index.
/// </summary>
/// <param name="Column">The containing projected column.</param>
/// <param name="JsonSegments">The path within the JSON column.</param>
/// <param name="Type">The scalar schema type.</param>
/// <param name="IsArray">Whether the path traverses an array.</param>
/// <param name="Descending">Whether to sort descending.</param>
internal sealed record ReadModelIndexTarget(ProjectedColumn Column, string[] JsonSegments, JsonObjectType Type, bool IsArray, bool Descending)
{
    /// <summary>
    /// Gets whether the key requires JSON extraction.
    /// </summary>
    internal bool IsExpression => JsonSegments.Length > 0;

    /// <summary>
    /// Resolves a declared path against the schema.
    /// </summary>
    /// <param name="property">The indexed property.</param>
    /// <param name="schema">The read-model schema.</param>
    /// <param name="columns">The projected columns.</param>
    /// <returns>The resolved target, or null when unsupported.</returns>
    internal static ReadModelIndexTarget? Resolve(IndexedProperty property, JsonSchema schema, IReadOnlyList<ProjectedColumn> columns)
    {
        var segments = property.Path.Segments.ToArray();
        if (segments.Length == 0 || segments.Any(segment => segment is not PropertyName || string.IsNullOrEmpty(segment.Value))) return null;
        var names = segments.Select(segment => segment.Value).ToArray();
        var column = columns.FirstOrDefault(column => column.Name == names[0]);
        if (column is null || (names.Length > 1 && !column.IsJson)) return null;
        if (column.IsKey && names.Length == 1) return new(column, [], JsonObjectType.String, false, property.Descending);
        var current = schema.ActualSchema;
        var array = false;
        foreach (var name in names)
        {
            if (current.Type.HasFlag(JsonObjectType.Array))
            {
                array = true;
                if (current.Item is null) return null;
                current = current.Item.ActualSchema;
            }
            var child = current.GetFlattenedProperties().FirstOrDefault(child => child.Name == name);
            if (child is null) return null;
            current = child.ActualSchema;
        }
        if (array || current.Type.HasFlag(JsonObjectType.Array)) return new(column, [], JsonObjectType.Array, true, property.Descending);
        var type = current.Type & ~JsonObjectType.Null;
        if (type is not (JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean)) return null;
        return new(column, names[1..], type, false, property.Descending);
    }
}
