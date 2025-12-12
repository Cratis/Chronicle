// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFutures"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionFutures)]
public class ProjectionFutures : Grain<ProjectionFuturesState>, IProjectionFutures
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionFuture>> GetFutures() => State.Futures;

    /// <inheritdoc/>
    public async Task AddFuture(ProjectionFuture future)
    {
        var debugMsg = $"{DateTime.Now:HH:mm:ss.fff} ProjectionFutures.AddFuture called: FutureId={future.Id}, Event={future.Event.Context.EventType}, SequenceNumber={future.Event.Context.SequenceNumber}, CurrentFuturesCount={State.Futures.Count}\n";
        System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", debugMsg);

        var existing = State.Futures.FirstOrDefault(f => f.Event.Context.SequenceNumber == future.Event.Context.SequenceNumber);
        if (existing is not null)
        {
            var skipMsg = $"{DateTime.Now:HH:mm:ss.fff} SKIPPING DUPLICATE: FutureId={future.Id} matches existing FutureId={existing.Id} with SequenceNumber={future.Event.Context.SequenceNumber}\n";
            System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", skipMsg);
            return;
        }

        State.Futures.Add(future);
        State.AddedFutures.Add(future);
        await WriteStateAsync();

        var addedMsg = $"{DateTime.Now:HH:mm:ss.fff} ADDED FUTURE: FutureId={future.Id}, NewCount={State.Futures.Count}\n";
        System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", addedMsg);
    }

    /// <inheritdoc/>
    public async Task ResolveFuture(ProjectionFutureId futureId)
    {
        var debugMsg = $"{DateTime.Now:HH:mm:ss.fff} ProjectionFutures.ResolveFuture called: FutureId={futureId}, CurrentFuturesCount={State.Futures.Count}\n";
        System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", debugMsg);

        var future = State.Futures.FirstOrDefault(f => f.Id == futureId);
        if (future is not null)
        {
            State.Futures.Remove(future);
            State.ResolvedFutures.Add(future);
            await WriteStateAsync();

            var removedMsg = $"{DateTime.Now:HH:mm:ss.fff} REMOVED FUTURE: FutureId={futureId}, NewCount={State.Futures.Count}\n";
            System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", removedMsg);
        }
        else
        {
            var notFoundMsg = $"{DateTime.Now:HH:mm:ss.fff} FUTURE NOT FOUND: FutureId={futureId}\n";
            System.IO.File.AppendAllText("/tmp/resolve_futures_debug.log", notFoundMsg);
        }
    }
}
