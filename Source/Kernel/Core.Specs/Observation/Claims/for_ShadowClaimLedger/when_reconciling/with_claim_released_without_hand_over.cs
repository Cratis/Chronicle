// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

public class with_claim_released_without_hand_over : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);

    [Fact] void should_remove_claim() => _ledger.Snapshot().Claims.ShouldBeEmpty();
    [Fact] void should_report_release_without_hand_over() => _divergences.ShouldContain((_partition, ShadowClaimDivergence.ReleasedWithoutHandOver));
    [Fact] void should_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(2UL);
}
