// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

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
    /// <returns>True if it exists, false if not.</returns>
    /// <typeparam name="TReadModel">Type of read model to check for.</typeparam>
    bool HasFor<TReadModel>();

    /// <summary>
    /// Check if there is a definition for a specific type.
    /// </summary>
    /// <param name="readModelType">Type of read model to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasFor(Type readModelType);

    /// <summary>
    /// Get all registered handlers.
    /// </summary>
    /// <returns>Collection of <see cref="IProjectionHandler"/>.</returns>
    IEnumerable<IProjectionHandler> GetAllHandlers();

    /// <summary>
    /// Get the <see cref="IProjectionHandler"/> for a specific projection type.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection to get for.</typeparam>
    /// <returns><see cref="IProjectionHandler"/> for the projection.</returns>
    IProjectionHandler GetHandlerFor<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Get the <see cref="ProjectionId"/> for a specific type.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection to get for.</typeparam>
    /// <returns>The <see cref="ProjectionId"/> for the type.</returns>
    ProjectionId GetProjectionIdFor<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Get the <see cref="ProjectionId"/> for a specific type.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to get for.</typeparam>
    /// <returns>The <see cref="ProjectionId"/> for the type.</returns>
    ProjectionId GetProjectionIdForModel<TReadModel>();

    /// <summary>
    /// Get the <see cref="ProjectionId"/> for a specific type.
    /// </summary>
    /// <param name="readModelType">Type of read model to get for.</param>
    /// <returns>The <see cref="ProjectionId"/> for the type.</returns>
    ProjectionId GetProjectionIdForModel(Type readModelType);

    /// <summary>
    /// Get any failed partitions for a specific projection.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection.</typeparam>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Get any failed partitions for a specific projection.
    /// </summary>
    /// <param name="projectionType">Type of projection.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(Type projectionType);

    /// <summary>
    /// Get the state of a specific projection.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection get for.</typeparam>
    /// <returns><see cref="ProjectionState"/>.</returns>
    Task<ProjectionState> GetStateFor<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Replay a specific projection.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection to replay.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task Replay<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Replay a specific projection by its identifier.
    /// </summary>
    /// <param name="projectionId"><see cref="ProjectionId"/> to replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replay(ProjectionId projectionId);

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
