// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Concepts.Observation.Reactors.Json;

/// <summary>
/// Defines a parser for JSON definition of a projection.
/// </summary>
public interface IJsonReactorDefinitionSerializer
{
    /// <summary>
    /// Deserialize a JSON string definition into <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="json">JSON to parse.</param>
    /// <returns><see cref="ReactorDefinition"/> instance.</returns>
    ReactorDefinition Deserialize(JsonNode json);

    /// <summary>
    /// Serialize a <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ReactorDefinition"/> to serialize.</param>
    /// <returns>JSON representation.</returns>
    JsonNode Serialize(ReactorDefinition definition);
}
