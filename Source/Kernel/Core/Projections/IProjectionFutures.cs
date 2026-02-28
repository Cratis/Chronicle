// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines projection futures management.
/// </summary>
public interface IProjectionFutures : IGrainWithStringKey
{
    /// <summary>
    /// Get all pending futures that need resolution.
    /// </summary>
    /// <returns>Collection of <see cref="ProjectionFuture"/> instances.</returns>
    Task<IEnumerable<ProjectionFuture>> GetFutures();

    /// <summary>
    /// Add a future that needs resolution.
    /// </summary>
    /// <param name="future">The <see cref="ProjectionFuture"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task AddFuture(ProjectionFuture future);

    /// <summary>
    /// Resolve a future that has been successfully resolved.
    /// </summary>
    /// <param name="futureId">The identifier of the future to resolve.</param>
    /// <returns>Awaitable task.</returns>
    Task ResolveFuture(ProjectionFutureId futureId);
}
