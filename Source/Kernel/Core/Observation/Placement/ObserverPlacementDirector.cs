// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cluster;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement;

/// <summary>
/// Places observer grains on compatible silos with the role, or with unknown roles.
/// </summary>
/// <param name="eligibility">The role eligibility selector.</param>
public class ObserverPlacementDirector(IRoleEligibility eligibility) : IPlacementDirector
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">No compatible silo has or may have the Observers role.</exception>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var eligible = eligibility.Select(context.GetCompatibleSilos(target), roles => roles.Observers, includeLackingRole: false);
        var selected = eligible.Silos.OrderBy(_ => Random.Shared.Next()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No silo has the Observers role or unknown roles for observer grain placement; " +
                "enable Cratis:Chronicle:Clustering:Roles:Observers on at least one compatible silo.");

        return Task.FromResult(selected);
    }
}
