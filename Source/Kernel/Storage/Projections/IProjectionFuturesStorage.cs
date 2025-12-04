// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
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
    /// <param name="key">The root key this future is associated with.</param>
    /// <param name="future">The future to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(ProjectionId projectionId, Key key, ProjectionFuture future);

    /// <summary>
    /// Get all futures for a projection and key.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="key">The root key to get futures for.</param>
    /// <returns>Collection of futures.</returns>
    Task<IEnumerable<ProjectionFuture>> GetForProjectionAndKey(ProjectionId projectionId, Key key);

    /// <summary>
    /// Remove a future.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="key">The root key.</param>
    /// <param name="futureId">The future identifier to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ProjectionId projectionId, Key key, ProjectionFutureId futureId);

    /// <summary>
    /// Remove all futures for a projection and key.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="key">The root key.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllForProjectionAndKey(ProjectionId projectionId, Key key);
}
