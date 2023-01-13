// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Defines a system that works with <see cref="IImmediateProjectionFor{TModel}"/>.
/// </summary>
public interface IImmediateProjections
{
    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and specific <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> defining the projection.</param>
    /// <returns>An instance for the id as a <see cref="JsonNode"/>..</returns>
    Task<ImmediateProjectionResult> GetInstanceById(ModelKey modelKey, ProjectionDefinition projectionDefinition);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <param name="projectionDefinition">Optional <see cref="ProjectionDefinition"/> defining the projection.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>An instance for the id.</returns>
    Task<ImmediateProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey, ProjectionDefinition? projectionDefinition = default);
}
