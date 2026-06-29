// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Storage.InMemory.Projections;

/// <summary>
/// Represents an in-memory implementation of <see cref="IProjectionFuturesStorage"/>.
/// </summary>
public sealed class ProjectionFuturesStorage : IProjectionFuturesStorage
{
    readonly ConcurrentDictionary<ProjectionId, ConcurrentDictionary<ProjectionFutureId, ProjectionFuture>> _futures = new();

    /// <inheritdoc/>
    public Task Save(ProjectionId projectionId, ProjectionFuture future)
    {
        var futures = _futures.GetOrAdd(projectionId, _ => new());
        futures[future.Id] = future;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetForProjection(ProjectionId projectionId) =>
        Task.FromResult<IEnumerable<ProjectionFuture>>(
            _futures.TryGetValue(projectionId, out var futures)
                ? futures.Values.ToArray()
                : []);

    /// <inheritdoc/>
    public Task Remove(ProjectionId projectionId, ProjectionFutureId futureId)
    {
        if (_futures.TryGetValue(projectionId, out var futures))
        {
            futures.TryRemove(futureId, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAllForProjection(ProjectionId projectionId)
    {
        _futures.TryRemove(projectionId, out _);
        return Task.CompletedTask;
    }
}
