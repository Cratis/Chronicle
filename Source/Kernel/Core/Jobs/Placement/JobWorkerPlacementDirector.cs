// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cluster;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement;

/// <summary>
/// Places job workers on role holders, unknown silos, or fallback silos in that order.
/// </summary>
/// <param name="eligibility">The role eligibility selector.</param>
/// <param name="options">The local Chronicle configuration.</param>
/// <param name="logger">The logger.</param>
public class JobWorkerPlacementDirector(IRoleEligibility eligibility, IOptions<ChronicleOptions> options, ILogger<JobWorkerPlacementDirector> logger) : IPlacementDirector
{
    int _usingFallback;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">No compatible silo is eligible for job worker placement.</exception>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var eligible = eligibility.Select(context.GetCompatibleSilos(target), roles => roles.Jobs, includeLackingRole: options.Value.Clustering.JobsFallback == JobsFallback.AnySilo);
        var selected = eligible.Silos.OrderBy(_ => Random.Shared.Next()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No compatible silo is eligible for the Jobs role; " +
                "enable Cratis:Chronicle:Clustering:Roles:Jobs on at least one compatible silo.");
        ReportFallback(eligible.Tier);

        return Task.FromResult(selected);
    }

    void ReportFallback(RoleEligibilityTier tier)
    {
        var usingFallback = tier == RoleEligibilityTier.LacksRole ? 1 : 0;
        if (Interlocked.Exchange(ref _usingFallback, usingFallback) != usingFallback)
        {
            if (usingFallback == 1)
            {
                logger.EnteringFallback();
            }
            else
            {
                logger.LeavingFallback();
            }
        }
    }
}
