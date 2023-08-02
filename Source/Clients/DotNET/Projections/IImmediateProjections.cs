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
    /// Gets all the definitions.
    /// </summary>
    IEnumerable<ProjectionDefinition> Definitions { get; }

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type.
    /// </summary>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>An instance for the id as a <see cref="JsonNode"/>..</returns>
    Task<ImmediateProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type specified as generic parameter.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>An instance for the id.</returns>
    Task<ImmediateProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> for a specific <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="identifier"><see cref="ProjectionId"/> to get for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>An instance for the id.</returns>
    Task<ImmediateProjectionResult> GetInstanceById(ProjectionId identifier, ModelKey modelKey);
}
