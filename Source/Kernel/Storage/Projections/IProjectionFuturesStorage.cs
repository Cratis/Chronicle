// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Projections;

/// <summary>
/// Defines a storage for projection futures.
/// </summary>
public interface IProjectionFuturesStorage
{
    /// <summary>
    /// Save a future for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="future">The future to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(ProjectionId projectionId, ProjectionFuture future);

    /// <summary>
    /// Get all futures for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>Collection of futures.</returns>
    Task<IEnumerable<ProjectionFuture>> GetForProjection(ProjectionId projectionId);

    /// <summary>
    /// Remove a future.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="futureId">The future identifier to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ProjectionId projectionId, ProjectionFutureId futureId);

    /// <summary>
    /// Remove all futures for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllForProjection(ProjectionId projectionId);
}
