// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Represents a placement director for server instance diagnostics that places the grain on the silo
/// its key names, making the reported metrics local to each silo.
/// </summary>
public class ServerInstancePlacementDirector : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var targetSiloAddress = SiloAddress.FromParsableString(target.GrainIdentity.Key.ToString());

        // When the silo named by the key is no longer part of the cluster, fall back to a local
        // activation rather than fault the call - the caller sees a fresh (if not entirely
        // meaningful) snapshot instead of an error for a silo that just left the cluster.
        var silo = Array.Exists(context.GetCompatibleSilos(target), address => address.Equals(targetSiloAddress))
            ? targetSiloAddress
            : context.LocalSilo;
        return Task.FromResult(silo);
    }
}
