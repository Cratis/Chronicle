// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Cluster.for_RoleEligibility.when_selecting;

public class with_local_silo_role_disabled_and_stale_metadata : given.a_role_placement
{
    EligibleSilos _result;

    void Establish()
    {
        _options.Clustering.Roles.Observers = false;
        _siloRoles.GetRoles(_local).Returns(new ClusterRoles());
    }

    void Because() => _result = _eligibility.Select([_local, _unknown], roles => roles.Observers, includeLackingRole: false);

    [Fact] void should_select_the_expected_tier() => _result.Tier.ShouldEqual(RoleEligibilityTier.Unknown);
    [Fact] void should_select_only_the_expected_silos() => _result.Silos.ShouldContainOnly(_unknown);
    [Fact] void should_not_read_local_metadata() => _siloRoles.DidNotReceive().GetRoles(_local);
}
