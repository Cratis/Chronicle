// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

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
        var schema = readModel.GetSchemaForLatestGeneration();
        var id = schema.Properties.FirstOrDefault(_ => _.Key.Equals("id", StringComparison.OrdinalIgnoreCase)).Value;
        if (id is not null)
        {
            var reference = JsonNode.Parse(id.ToJson())?["$ref"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(reference))
            {
                return reference.Split('/')[^1];
            }
            if (!string.IsNullOrWhiteSpace(id.Title))
            {
                return id.Title;
            }
        }
        return "CompositeKey";
    }
}
