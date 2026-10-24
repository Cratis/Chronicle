// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_comparing;

public class with_claim_without_set : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => _divergences = _ledger.Compare(_replaying, _catchingUp, false);

    [Fact] void should_report_orphan_claim() => _divergences.ShouldContain((_partition, ShadowClaimDivergence.ClaimWithoutDerivedSet));
    [Fact] void should_not_remove_claim() => Claim().Partition.ShouldEqual(_partition);
}
