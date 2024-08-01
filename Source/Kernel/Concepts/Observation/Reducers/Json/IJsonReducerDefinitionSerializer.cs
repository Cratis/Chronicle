// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Observation.Reducers.Json;

/// <summary>
/// Defines a parser for JSON definition of a projection.
/// </summary>
public interface IJsonReducerDefinitionSerializer
{
    /// <summary>
    /// Deserialize a JSON string definition into <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="json">JSON to parse.</param>
    /// <returns><see cref="ReducerDefinition"/> instance.</returns>
    ReducerDefinition Deserialize(JsonNode json);

    /// <summary>
    /// Serialize a <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to serialize.</param>
    /// <returns>JSON representation.</returns>
    JsonNode Serialize(ReducerDefinition definition);
}
