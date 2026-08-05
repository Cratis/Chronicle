// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
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
    /// <param name="projectionType">Type of projection, or the type of read model it projects to.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    /// <remarks>
    /// This one accepts either handle. It only ever resolved read model types, so narrowing it to what its parameter
    /// name says would break every caller it has - and model-bound projections, which have no projection type at all,
    /// would lose their only handle. Prefer <see cref="GetFailedPartitionsForModel(Type)"/> or
    /// <see cref="GetFailedPartitionsFor{TProjection}"/>, which each say which handle they take.
    /// </remarks>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(Type projectionType);

    /// <summary>
    /// Get any failed partitions for the projection that maintains a specific read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to get for.</typeparam>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsForModel<TReadModel>();

    /// <summary>
    /// Get any failed partitions for the projection that maintains a specific read model.
    /// </summary>
    /// <param name="readModelType">Type of read model to get for.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsForModel(Type readModelType);

    /// <summary>
    /// Get the state of a specific projection.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection get for.</typeparam>
    /// <returns><see cref="ProjectionState"/>.</returns>
    /// <remarks>
    /// A model-bound projection has no projection type to name here - use
    /// <see cref="GetStateForModel{TReadModel}"/> for those.
    /// </remarks>
    Task<ProjectionState> GetStateFor<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Get the state of the projection that maintains a specific read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to get for.</typeparam>
    /// <returns><see cref="ProjectionState"/>.</returns>
    /// <remarks>
    /// Model-bound projections are declared on the read model and have no type of their own, so this is the only way
    /// to ask for their state. Fluent projections answer to it as well, by the model they project to.
    /// </remarks>
    Task<ProjectionState> GetStateForModel<TReadModel>();

    /// <summary>
    /// Get the state of the projection that maintains a specific read model.
    /// </summary>
    /// <param name="readModelType">Type of read model to get for.</param>
    /// <returns><see cref="ProjectionState"/>.</returns>
    Task<ProjectionState> GetStateForModel(Type readModelType);

    /// <summary>
    /// Replay a specific projection.
    /// </summary>
    /// <typeparam name="TProjection">Type of projection to replay.</typeparam>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the projection is not replayable.</returns>
    Task<JobId> Replay<TProjection>()
        where TProjection : IProjection;

    /// <summary>
    /// Replay a specific projection by its identifier.
    /// </summary>
    /// <param name="projectionId"><see cref="ProjectionId"/> to replay.</param>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the projection is not replayable.</returns>
    Task<JobId> Replay(ProjectionId projectionId);

    /// <summary>
    /// Get the external event store subscription requirements derived from all discovered projection definitions.
    /// </summary>
    /// <remarks>
    /// Returns one entry per external event store name, collecting all event type identifiers used by
    /// projections whose event sequence is an inbox sequence for that store.
    /// </remarks>
    /// <returns>A collection of tuples mapping external event store names to their event type identifiers.</returns>
    IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> GetExternalEventStoreSubscriptions();

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
    /// Query a projection declaration against the event log without registering it.
    /// </summary>
    /// <remarks>
    /// The declaration may omit the <c>=> ReadModelType</c> target — in that case the read model
    /// schema is inferred from the events used in the projection. An inferred read model can never
    /// be registered as a permanent projection; query-only declarations are exclusively for
    /// ad-hoc exploration.
    /// </remarks>
    /// <param name="declaration">The Projection Declaration Language string to query.</param>
    /// <param name="eventSequenceId">Optional event sequence identifier to query. Defaults to <c>"event-log"</c>.</param>
    /// <returns>A <see cref="ProjectionQueryResult"/> containing the resulting read model entries.</returns>
    /// <exception cref="UnableToQueryProjection">Thrown when the declaration contains errors.</exception>
    Task<ProjectionQueryResult> Query(string declaration, string eventSequenceId = "event-log");
}
