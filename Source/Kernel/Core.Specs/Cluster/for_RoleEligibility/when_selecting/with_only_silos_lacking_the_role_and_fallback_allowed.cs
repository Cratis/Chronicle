// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Cluster.for_RoleEligibility.when_selecting;

public class with_only_silos_lacking_the_role_and_fallback_allowed : given.a_role_placement
{
    EligibleSilos _result;

    void Establish()
    {
        _options.Clustering.Roles.Observers = false;
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles { Observers = false });
    }

    void Because() => _result = _eligibility.Select([_local, _remote], roles => roles.Observers, includeLackingRole: true);

    [Fact] void should_select_the_expected_tier() => _result.Tier.ShouldEqual(RoleEligibilityTier.LacksRole);
    [Fact] void should_select_only_the_expected_silos() => _result.Silos.ShouldContainOnly(_local, _remote);
}
