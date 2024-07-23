// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that works with projections in the system.
/// </summary>
public interface IProjections
{
    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
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
    /// <returns>The result of the projection in the form of an <see cref="ImmediateProjectionResult"/>.</returns>
    Task<ImmediateProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> for a specific <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="identifier"><see cref="ProjectionId"/> to get for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>The raw result of the projection in the form of an <see cref="ImmediateProjectionResultRaw"/>.</returns>
    Task<ImmediateProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type specified as generic parameter.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>The result of the projection in the form of an <see cref="ImmediateProjectionResult"/>.</returns>
    Task<ImmediateProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type for the current session.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>The result of the projection in the form of an <see cref="ImmediateProjectionResult"/>.</returns>
    Task<ImmediateProjectionResult> GetInstanceByIdForSession(CorrelationId correlationId, Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get the current instance for a specific <see cref="ModelKey"/> and type for the current session and apply events to it.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <param name="events">Collection of events to apply.</param>
    /// <returns>The result of the projection in the form of an <see cref="ImmediateProjectionResult"/>.</returns>
    Task<ImmediateProjectionResult> GetInstanceByIdForSessionWithEventsApplied(CorrelationId correlationId, Type modelType, ModelKey modelKey, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate a session.
    /// </summary>
    /// <param name="correlationId">The correlation identifier for the session.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(CorrelationId correlationId, Type modelType, ModelKey modelKey);

    /// <summary>
    /// Discover all projections from entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all projections with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();
}
