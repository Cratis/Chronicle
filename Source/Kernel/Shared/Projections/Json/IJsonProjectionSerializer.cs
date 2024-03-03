// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Projections.Definitions;

namespace Cratis.Projections.Json;

/// <summary>
/// Defines a parser for JSON definition of a projection.
/// </summary>
public interface IJsonProjectionSerializer
{
    /// <summary>
    /// Deserialize a JSON string definition into <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="json">JSON to parse.</param>
    /// <returns><see cref="ProjectionDefinition"/> instance.</returns>
    ProjectionDefinition Deserialize(JsonNode json);

    /// <summary>
    /// Serialize a <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to serialize.</param>
    /// <returns>JSON representation.</returns>
    JsonNode Serialize(ProjectionDefinition definition);
}
