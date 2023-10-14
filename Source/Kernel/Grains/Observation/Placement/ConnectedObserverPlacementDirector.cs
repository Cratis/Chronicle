// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;
using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace Aksio.Cratis.Kernel.Grains.Observation.Placement;

/// <summary>
/// Represents a placement director for connected observers to guarantee they run on the same silo as a connected client.
/// </summary>
public class ConnectedObserverPlacementDirector : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        target.GrainIdentity.GetGuidKey(out var key);
        var connectedObserverKey = ObserverSubscriberKey.Parse(key!);
        var targetSiloAddress = SiloAddress.FromParsableString(connectedObserverKey.SiloAddress);
        return Task.FromResult(targetSiloAddress);
    }
}
