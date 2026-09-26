// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// Finds a composite key type name in the read-model schema for legacy untyped expressions.
/// </summary>
public static class CompositeKeyTypeName
{
    /// <summary>
    /// Gets the type name for the read model's Id property, or a stable fallback.
    /// </summary>
    /// <param name="readModel">The read model whose key is being generated.</param>
    /// <returns>The type name.</returns>
    public static string From(ReadModelDefinition readModel)
    {
        return From(readModel.GetSchemaForLatestGeneration());
    }

    /// <summary>
    /// Gets the Id type from a read model or child item schema.
    /// </summary>
    /// <param name="schema">The schema containing the Id property.</param>
    /// <returns>The type name, or a best-effort fallback when the schema does not declare one.</returns>
    public static string From(JsonSchema schema)
    {
        var id = schema.Properties.FirstOrDefault(_ => _.Key.Equals("id", StringComparison.OrdinalIgnoreCase)).Value;
        if (id is not null)
        {
            var actual = id.OneOf.Select(_ => _.ActualSchema).FirstOrDefault(_ => _.Type.HasFlag(JsonObjectType.Object))
                ?? id.ActualSchema;
            if (!string.IsNullOrWhiteSpace(actual.Title))
            {
                return actual.Title;
            }

            var reference = JsonNode.Parse(id.ToJson())?["$ref"]?.GetValue<string>()
                ?? id.OneOf.Concat(id.AllOf).Where(_ => _.HasReference)
                    .Select(_ => JsonNode.Parse(_.ToJson())?["$ref"]?.GetValue<string>()).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(reference))
            {
                return reference.Split('/')[^1];
            }
            if (!string.IsNullOrWhiteSpace(id.Title))
            {
                return id.Title;
            }
        }

        // A primitive or untyped Id supplies no valid composite type. Keep legacy generation best-effort:
        // declarations using this placeholder require a CompositeKey object in the read model to recompile.
        return "CompositeKey";
    }
}
