// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_comparing;

public class with_set_without_claim : given.a_ledger
{
    void Establish() => _catchingUp.Add(_partition);
    void Because() => _divergences = _ledger.Compare(_replaying, _catchingUp, false);

    [Fact] void should_report_missing_claim() => _divergences.ShouldContain((_partition, ShadowClaimDivergence.DerivedSetWithoutClaim));
    [Fact] void should_not_create_claim() => _ledger.Snapshot().Claims.ShouldBeEmpty();
}
