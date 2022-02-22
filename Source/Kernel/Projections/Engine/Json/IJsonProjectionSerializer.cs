// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections.Json;

/// <summary>
/// Defines a parser for JSON definition of a <see cref="IProjection"/>.
/// </summary>
public interface IJsonProjectionSerializer
{
    /// <summary>
    /// Deserialize a JSON string definition into <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="json">JSON to parse.</param>
    /// <returns><see cref="ProjectionDefinition"/> instance.</returns>
    ProjectionDefinition Deserialize(string json);

    /// <summary>
    /// Serialize a <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to serialize.</param>
    /// <returns>JSON representation.</returns>
    string Serialize(ProjectionDefinition definition);
}
