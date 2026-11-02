// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cluster;
using Cratis.Chronicle.Releases;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement;

/// <summary>
/// Places observer grains on compatible silos with the role, or with unknown roles.
/// </summary>
/// <param name="eligibility">The role eligibility selector.</param>
/// <param name="clusterReleases">The published kernel releases used to avoid older silos during a rollout.</param>
/// <remarks>
/// The role is a hard constraint: only silos with the Observers role, or with unknown roles, are candidates.
/// Among those, in a mixed-release cluster observers prefer silos at least as new as this one, so older silos do not
/// receive subscriber types they cannot load. If no such observer host exists, the role-eligible silos are used.
/// </remarks>
public class ObserverPlacementDirector(IRoleEligibility eligibility, IClusterReleases clusterReleases) : IPlacementDirector
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">No compatible silo has or may have the Observers role.</exception>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var eligible = eligibility.Select(context.GetCompatibleSilos(target), roles => roles.Observers, includeLackingRole: false).Silos;
        var preferred = clusterReleases.AtLeastAsNewAsThisSilo(eligible);
        IEnumerable<SiloAddress> candidates = preferred.Count > 0 ? preferred : eligible;

        var selected = candidates.OrderBy(_ => Random.Shared.Next()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No silo has the Observers role or unknown roles for observer grain placement; " +
                "enable Cratis:Chronicle:Clustering:Roles:Observers on at least one compatible silo.");

        return Task.FromResult(selected);
    }
}
