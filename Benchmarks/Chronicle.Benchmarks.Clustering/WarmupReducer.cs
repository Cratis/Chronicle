// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Reducer the fixture waits on to confirm that the client connection, artifact registration and
/// cross-silo observer activation have all completed.
/// </summary>
public class WarmupReducer : IReducerFor<WarmupReadModel>
{
    /// <summary>
    /// Handles the <see cref="ClusterWarmedUp"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current read model.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated read model.</returns>
    public Task<WarmupReadModel?> OnClusterWarmedUp(ClusterWarmedUp @event, WarmupReadModel? current, EventContext context)
    {
        _ = current;
        _ = context;

        return Task.FromResult<WarmupReadModel?>(new(@event.Value));
    }
}
