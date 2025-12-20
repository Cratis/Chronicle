// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

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
    /// Get an instance by a specific <see cref="ReadModelKey"/> and type.
    /// </summary>
    /// <param name="readModelType">Type of read model the projection is for.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceById(Type readModelType, ReadModelKey readModelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ReadModelKey"/> for a specific <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="identifier"><see cref="ProjectionId"/> to get for.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <returns>The raw result of the projection in the form of an <see cref="ProjectionResultRaw"/>.</returns>
    Task<ProjectionResultRaw> GetInstanceById(ProjectionId identifier, ReadModelKey readModelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ReadModelKey"/> and type specified as generic parameter.
    /// </summary>
    /// <param name="modelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult<TReadModel>> GetInstanceById<TReadModel>(ReadModelKey modelKey);

    /// <summary>
    /// Get an instance by a specific <see cref="ReadModelKey"/> and type for the current session.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to get for.</param>
    /// <param name="readModelType">Type of read model the projection is for.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceByIdForSession(ProjectionSessionId sessionId, Type readModelType, ReadModelKey readModelKey);

    /// <summary>
    /// Get the current instance for a specific <see cref="ReadModelKey"/> and type for the current session and apply events to it.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to get for.</param>
    /// <param name="readModelType">Type of read model the projection is for.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <param name="events">Collection of events to apply.</param>
    /// <returns>The result of the projection in the form of an <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(ProjectionSessionId sessionId, Type readModelType, ReadModelKey readModelKey, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate a session.
    /// </summary>
    /// <param name="sessionId">The <see cref="ProjectionSessionId"/> to dehydrate.</param>
    /// <param name="readModelType">Type of read model the projection is for.</param>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get instance for.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(ProjectionSessionId sessionId, Type readModelType, ReadModelKey readModelKey);

    /// <summary>
    /// Observe changes for a specific model with optionally a specific <see cref="ReadModelKey"/>.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to observe changes for.</typeparam>
    /// <returns>An observable of <see cref="ProjectionChangeset{TReadModel}"/>.</returns>
    IObservable<ProjectionChangeset<TReadModel>> Watch<TReadModel>();

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

    /// <summary>
    /// Get snapshots of a projection grouped by CorrelationId by walking through events from the beginning.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <param name="readModelKey"><see cref="ReadModelKey"/> to get snapshots for.</param>
    /// <returns>Collection of <see cref="ProjectionSnapshot{TReadModel}"/>.</returns>
    Task<IEnumerable<ProjectionSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey);
}
