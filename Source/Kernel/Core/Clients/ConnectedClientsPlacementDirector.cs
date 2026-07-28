// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents a placement director for connected clients tracking that places the grain on the silo
/// its key names, making the tracking local to each silo.
/// </summary>
public class ConnectedClientsPlacementDirector : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var targetSiloAddress = SiloAddress.FromParsableString(target.GrainIdentity.Key.ToString());

        // When the silo named by the key is no longer part of the cluster, fall back to a local
        // activation. Its registry starts empty, correctly reporting every client that was connected
        // to the dead silo as no longer connected.
        var silo = Array.Exists(context.GetCompatibleSilos(target), address => address.Equals(targetSiloAddress))
            ? targetSiloAddress
            : context.LocalSilo;
        return Task.FromResult(silo);
    }
}
