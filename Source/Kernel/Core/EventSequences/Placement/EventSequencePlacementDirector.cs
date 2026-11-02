// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cluster;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.EventSequences.Placement;

/// <summary>
/// Places event sequence grains on compatible silos with the role, or with unknown roles.
/// </summary>
/// <param name="eligibility">The role eligibility selector.</param>
public class EventSequencePlacementDirector(IRoleEligibility eligibility) : IPlacementDirector
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">No compatible silo has or may have the EventSequences role.</exception>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var eligible = eligibility.Select(context.GetCompatibleSilos(target), roles => roles.EventSequences, includeLackingRole: false);
        var selected = eligible.Silos.OrderBy(_ => Random.Shared.Next()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No silo has the EventSequences role or unknown roles for event sequence grain placement; " +
                "enable Cratis:Chronicle:Clustering:Roles:EventSequences on at least one compatible silo.");

        return Task.FromResult(selected);
    }
}
