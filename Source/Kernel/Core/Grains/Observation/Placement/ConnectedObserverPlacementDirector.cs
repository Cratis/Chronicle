// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Grains.Observation.Placement;

/// <summary>
/// Represents a placement director for connected observers to guarantee they run on the same silo as a connected client.
/// </summary>
public class ConnectedObserverPlacementDirector : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var connectedObserverKey = ObserverSubscriberKey.Parse(target.GrainIdentity.Key.ToString()!);
        var targetSiloAddress = SiloAddress.FromParsableString(connectedObserverKey.SiloAddress);
        return Task.FromResult(targetSiloAddress);
    }
}
