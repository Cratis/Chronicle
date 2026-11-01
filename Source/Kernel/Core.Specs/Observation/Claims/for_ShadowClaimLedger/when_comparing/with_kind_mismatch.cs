// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_comparing;

public class with_kind_mismatch : given.a_ledger
{
    void Establish()
    {
        GrantReplay();
        _catchingUp.Add(_partition);
    }

    void Because() => _divergences = _ledger.Compare(_replaying, _catchingUp, false);

    [Fact] void should_report_only_kind_mismatch() => _divergences.ShouldContainOnly((_partition, ShadowClaimDivergence.KindMismatch));
    [Fact] void should_not_reissue_claim() => Claim().Epoch.Value.ShouldEqual(1UL);
}
