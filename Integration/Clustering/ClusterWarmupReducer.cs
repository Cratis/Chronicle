// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Reducer whose activation and event handling the fixture waits on to confirm the cluster is fully
/// operational (connection established, artifacts registered, observers active across silos) before tests.
/// </summary>
public class ClusterWarmupReducer : IReducerFor<ClusterWarmupReadModel>
{
    /// <summary>
    /// Handles the <see cref="ClusterWarmedUp"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current read model.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated read model.</returns>
    public Task<ClusterWarmupReadModel?> OnClusterWarmedUp(ClusterWarmedUp @event, ClusterWarmupReadModel? current, EventContext context) =>
        Task.FromResult<ClusterWarmupReadModel?>(new(@event.Value));
}
