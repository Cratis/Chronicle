// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cluster.for_RoleEligibility.when_selecting;

public class with_local_silo_role_enabled_and_no_metadata : given.a_role_placement
{
    EligibleSilos _result;

    void Because() => _result = _eligibility.Select([_local, _unknown], roles => roles.Observers, includeLackingRole: false);

    [Fact] void should_select_the_expected_tier() => _result.Tier.ShouldEqual(RoleEligibilityTier.HasRole);
    [Fact] void should_select_only_the_expected_silos() => _result.Silos.ShouldContainOnly(_local);
    [Fact] void should_not_read_local_metadata() => _siloRoles.DidNotReceive().GetRoles(_local);
}
