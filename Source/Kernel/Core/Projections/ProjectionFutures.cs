// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Orleans.Providers;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFutures"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionFutures)]
public class ProjectionFutures : Grain<ProjectionFuturesState>, IProjectionFutures
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures() =>
        Task.FromResult<IEnumerable<ProjectionFuture>>(State.Futures);

    /// <inheritdoc/>
    public Task<int> AddFuture(ProjectionFuture future) => AddFuture(future, pinned: false);

    /// <inheritdoc/>
    public Task<int> AddPinnedFuture(ProjectionFuture future) => AddFuture(future, pinned: true);

    /// <inheritdoc/>
    public async Task ResolveFuture(ProjectionFutureId futureId)
    {
        var future = State.Futures.FirstOrDefault(f => f.Id == futureId);
        var previousState = State;
        if (future is not null)
        {
            State.ResolvedFutures.Add(future);
        }

        try
        {
            await WriteStateAsync();
            if (future is not null)
            {
                State.Futures.Remove(future);
            }
        }
        catch
        {
            State = previousState;
            throw;
        }
        finally
        {
            State.ResolvedFutures.Clear();
        }
    }

    async Task<int> AddFuture(ProjectionFuture future, bool pinned)
    {
        if (State.Futures.Any(f => f.Event.Context.SequenceNumber == future.Event.Context.SequenceNumber &&
            (!pinned || f.Event.Context.EventType == future.Event.Context.EventType)))
        {
            return State.Futures.Count;
        }

        var previousState = State;
        State.AddedFutures.Add(future);
        try
        {
            await WriteStateAsync();
            State.Futures.Add(future);
        }
        catch
        {
            State = previousState;
            throw;
        }
        finally
        {
            State.AddedFutures.Clear();
        }

        return State.Futures.Count;
    }
}
