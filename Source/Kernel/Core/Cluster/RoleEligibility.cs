// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Selects the highest available role tier, using authoritative configuration for the local silo.
/// </summary>
/// <param name="siloRoles">The advertised remote roles.</param>
/// <param name="localSiloDetails">The local silo's identity.</param>
/// <param name="options">The local Chronicle configuration.</param>
public class RoleEligibility(ISiloRoles siloRoles, ILocalSiloDetails localSiloDetails, IOptions<ChronicleOptions> options) : IRoleEligibility
{
    /// <inheritdoc/>
    public EligibleSilos Select(IEnumerable<SiloAddress> candidates, Func<ClusterRoles, bool> hasRole, bool includeLackingRole)
    {
        var tiers = candidates.ToLookup(silo => Classify(silo, hasRole));
        if (tiers[RoleEligibilityTier.HasRole].Any())
        {
            return new(RoleEligibilityTier.HasRole, tiers[RoleEligibilityTier.HasRole].ToArray());
        }

        if (tiers[RoleEligibilityTier.Unknown].Any())
        {
            return new(RoleEligibilityTier.Unknown, tiers[RoleEligibilityTier.Unknown].ToArray());
        }

        return new(RoleEligibilityTier.LacksRole, includeLackingRole ? tiers[RoleEligibilityTier.LacksRole].ToArray() : []);
    }

    RoleEligibilityTier Classify(SiloAddress silo, Func<ClusterRoles, bool> hasRole)
    {
        if (silo.Equals(localSiloDetails.SiloAddress))
        {
            return hasRole(options.Value.Clustering.Roles) ? RoleEligibilityTier.HasRole : RoleEligibilityTier.LacksRole;
        }

        if (!siloRoles.GetRoles(silo).TryGetValue(out var roles))
        {
            return RoleEligibilityTier.Unknown;
        }

        return hasRole(roles) ? RoleEligibilityTier.HasRole : RoleEligibilityTier.LacksRole;
    }
}
