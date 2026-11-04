// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Holds deferred operations for one isolated partition rebuild without accessing shared projection futures.
/// </summary>
internal sealed class PartitionRebuildFutures : IProjectionFuturesStore
{
    readonly List<ProjectionFuture> _futures = [];

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures() =>
        Task.FromResult<IEnumerable<ProjectionFuture>>(_futures.ToArray());

    /// <inheritdoc/>
    public Task<int> AddFuture(ProjectionFuture future) => AddFuture(future, pinned: false);

    /// <inheritdoc/>
    public Task<int> AddPinnedFuture(ProjectionFuture future) => AddFuture(future, pinned: true);

    /// <inheritdoc/>
    public Task ResolveFuture(ProjectionFutureId futureId)
    {
        var future = _futures.FirstOrDefault(_ => _.Id == futureId);
        if (future is not null)
        {
            _futures.Remove(future);
        }
        return Task.CompletedTask;
    }

    static bool IsDuplicate(ProjectionFuture existing, ProjectionFuture incoming, bool pinned)
    {
        if (existing.Event.Context.SequenceNumber != incoming.Event.Context.SequenceNumber)
        {
            return false;
        }
        if (!pinned)
        {
            return true;
        }
        return existing.Event.Context.EventType == incoming.Event.Context.EventType;
    }

    Task<int> AddFuture(ProjectionFuture future, bool pinned)
    {
        if (!_futures.Exists(_ => IsDuplicate(_, future, pinned)))
        {
            _futures.Add(future);
        }
        return Task.FromResult(_futures.Count);
    }
}
