// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Cluster.for_RoleEligibility.when_selecting;

public class with_default_roles_on_every_silo : given.a_role_placement
{
    EligibleSilos _result;

    void Establish()
    {
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles());
        _siloRoles.GetRoles(_unknown).Returns(new ClusterRoles());
    }

    void Because() => _result = _eligibility.Select([_local, _remote, _unknown], roles => roles.Observers, includeLackingRole: false);

    [Fact] void should_select_the_expected_tier() => _result.Tier.ShouldEqual(RoleEligibilityTier.HasRole);
    [Fact] void should_select_only_the_expected_silos() => _result.Silos.ShouldContainOnly(_local, _remote, _unknown);
}
