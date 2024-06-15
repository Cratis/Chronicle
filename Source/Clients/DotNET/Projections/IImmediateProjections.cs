// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that works with <see cref="IImmediateProjectionFor{TModel}"/>.
/// </summary>
public interface IImmediateProjections
{
    /// <summary>
    /// Gets all the definitions.
    /// </summary>
    IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <summary>
    /// Check if there is a definition for a specific type.
    /// </summary>
    /// <param name="modelType">Type of model to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasProjectionFor(Type modelType);

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
    /// <returns>A raw JSON instance for the id.</returns>
    Task<ImmediateProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type for the current session.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>An instance for the id as a <see cref="JsonNode"/>..</returns>
    Task<ImmediateProjectionResult> GetInstanceByIdForSession(CorrelationId correlationId, Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get the current instance for a specific <see cref="ModelKey"/> and type for the current session and apply events to it.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <param name="events">Collection of events to apply.</param>
    /// <returns>An instance for the id as a <see cref="JsonNode"/>..</returns>
    Task<ImmediateProjectionResult> GetInstanceByIdForSessionWithEventsApplied(CorrelationId correlationId, Type modelType, ModelKey modelKey, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate a session.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(CorrelationId correlationId, Type modelType, ModelKey modelKey);
}
