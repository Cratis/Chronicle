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
    public async Task<IEnumerable<ProjectionFuture>> GetFutures() => State.Futures;

    /// <inheritdoc/>
    public async Task AddFuture(ProjectionFuture future)
    {
        if (State.Futures.Any(f => f.Event.Context.SequenceNumber == future.Event.Context.SequenceNumber))
        {
            return;
        }

        State.Futures.Add(future);
        State.AddedFutures.Add(future);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task ResolveFuture(ProjectionFutureId futureId)
    {
        var future = State.Futures.FirstOrDefault(f => f.Id == futureId);
        if (future is not null)
        {
            State.Futures.Remove(future);
            State.ResolvedFutures.Add(future);
        }
        await WriteStateAsync();
    }
}
