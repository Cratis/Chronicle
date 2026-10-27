// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

public class with_kind_mismatch : given.a_ledger
{
    void Establish()
    {
        GrantReplay();
        _catchingUp.Add(_partition);
    }

    void Because() => _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);

    [Fact] void should_follow_the_derived_set_kind() => Claim().Holder.ShouldEqual(ClaimHolderKind.PartitionCatchUp);
    [Fact] void should_issue_a_new_epoch() => Claim().Epoch.Value.ShouldEqual(2UL);
    [Fact] void should_not_report_a_release() => _divergences.ShouldBeEmpty();
}
