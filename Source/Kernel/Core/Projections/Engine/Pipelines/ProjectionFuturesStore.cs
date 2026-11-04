// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Forwards pipeline futures operations to their existing projection futures grain.
/// </summary>
/// <param name="futures">The projection futures grain.</param>
internal sealed class ProjectionFuturesStore(IProjectionFutures futures) : IProjectionFuturesStore
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures() => futures.GetFutures();

    /// <inheritdoc/>
    public Task<int> AddFuture(ProjectionFuture future) => futures.AddFuture(future);

    /// <inheritdoc/>
    public Task<int> AddPinnedFuture(ProjectionFuture future) => futures.AddPinnedFuture(future);

    /// <inheritdoc/>
    public Task ResolveFuture(ProjectionFutureId futureId) => futures.ResolveFuture(futureId);
}
