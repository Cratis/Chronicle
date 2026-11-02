// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Selects compatible silos by their Chronicle roles.
/// </summary>
public interface IRoleEligibility
{
    /// <summary>
    /// Selects known role holders before unknown silos and optional fallback silos.
    /// </summary>
    /// <param name="candidates">The compatible silos.</param>
    /// <param name="hasRole">The role to require.</param>
    /// <param name="includeLackingRole">Whether silos lacking the role may be used as a last resort.</param>
    /// <returns>The highest nonempty tier, or an empty result when none is eligible.</returns>
    EligibleSilos Select(IEnumerable<SiloAddress> candidates, Func<ClusterRoles, bool> hasRole, bool includeLackingRole);
}
