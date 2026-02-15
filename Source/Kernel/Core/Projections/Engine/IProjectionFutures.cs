// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Defines a system for managing projection futures - deferred operations waiting for parent data to become available.
/// </summary>
public interface IProjectionFutures
{
    /// <summary>
    /// Get all pending futures for a projection that need resolution.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace to get futures for.</param>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>Collection of <see cref="ProjectionFuture"/> instances.</returns>
    Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId);

    /// <summary>
    /// Add a future that needs resolution.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace to add the future to.</param>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="future">The <see cref="ProjectionFuture"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task AddFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFuture future);

    /// <summary>
    /// Resolve a future that has been successfully resolved.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace to resolve the future in.</param>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="futureId">The identifier of the future to resolve.</param>
    /// <returns>Awaitable task.</returns>
    Task ResolveFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFutureId futureId);
}
