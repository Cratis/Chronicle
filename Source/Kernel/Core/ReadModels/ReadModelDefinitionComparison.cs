// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Compares read model definitions by content rather than schema and collection identity.
/// </summary>
internal static class ReadModelDefinitionComparison
{
    /// <summary>
    /// Determines whether two definitions have the same persisted content.
    /// </summary>
    /// <param name="first">The first definition.</param>
    /// <param name="second">The second definition.</param>
    /// <returns>Whether the definitions are equal.</returns>
    public static bool Equals(ReadModelDefinition first, ReadModelDefinition second) =>
        first == second with { Schemas = first.Schemas, Indexes = first.Indexes } &&
        first.Indexes.OrderBy(_ => _.PropertyPath.ToString()).SequenceEqual(second.Indexes.OrderBy(_ => _.PropertyPath.ToString())) &&
        first.Schemas.Count == second.Schemas.Count &&
        first.Schemas.All(pair => second.Schemas.TryGetValue(pair.Key, out var schema) && JsonNode.DeepEquals(JsonNode.Parse(pair.Value.ToJson()), JsonNode.Parse(schema.ToJson())));
}
