// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Defines pending futures storage for a projection pipeline independently of grain transport.
/// </summary>
public interface IProjectionFuturesStore
{
    /// <summary>
    /// Gets the pending futures.
    /// </summary>
    /// <returns>The pending futures.</returns>
    Task<IEnumerable<ProjectionFuture>> GetFutures();

    /// <summary>
    /// Adds a future, deduplicating by physical event sequence number.
    /// </summary>
    /// <param name="future">The future to add.</param>
    /// <returns>The pending futures count.</returns>
    Task<int> AddFuture(ProjectionFuture future);

    /// <summary>
    /// Adds a pinned future, retaining different event generations.
    /// </summary>
    /// <param name="future">The future to add.</param>
    /// <returns>The pending futures count.</returns>
    Task<int> AddPinnedFuture(ProjectionFuture future);

    /// <summary>
    /// Removes a successfully resolved future.
    /// </summary>
    /// <param name="futureId">The future identifier.</param>
    /// <returns>The awaitable task.</returns>
    Task ResolveFuture(ProjectionFutureId futureId);
}
