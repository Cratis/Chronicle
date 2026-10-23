// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cluster.for_RoleEligibility.when_selecting;

public class with_no_candidates : given.a_role_placement
{
    EligibleSilos _result;

    void Because() => _result = _eligibility.Select([], roles => roles.Observers, includeLackingRole: true);

    [Fact] void should_select_the_expected_tier() => _result.Tier.ShouldEqual(RoleEligibilityTier.LacksRole);
    [Fact] void should_not_select_a_silo() => _result.Silos.ShouldBeEmpty();
}
