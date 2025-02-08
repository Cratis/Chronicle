// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that works with projections in the system.
/// </summary>
public interface IProjections
{
    /// <summary>
    /// Check if there is a definition for a specific projection identifier.
    /// </summary>
    /// <param name="projectionId">Identifier of projection.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasFor(ProjectionId projectionId);

    /// <summary>
    /// Check if there is a definition for a specific type.
    /// </summary>
    /// <param name="modelType">Type of model to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasFor(Type modelType);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type.
    /// </summary>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> for a specific <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="identifier"><see cref="ProjectionId"/> to get for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>The raw result of the projection in the form of an <see cref="ProjectionResultRaw"/>.</returns>
    Task<ProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type specified as generic parameter.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/> and type for the current session.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to get for.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceByIdForSession(ProjectionSessionId sessionId, Type modelType, ModelKey modelKey);

    /// <summary>
    /// Get the current instance for a specific <see cref="ModelKey"/> and type for the current session and apply events to it.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to get for.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <param name="events">Collection of events to apply.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(ProjectionSessionId sessionId, Type modelType, ModelKey modelKey, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate a session.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to dehydrate.</param>
    /// <param name="modelType">Type of model the projection is for.</param>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(ProjectionSessionId sessionId, Type modelType, ModelKey modelKey);

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
