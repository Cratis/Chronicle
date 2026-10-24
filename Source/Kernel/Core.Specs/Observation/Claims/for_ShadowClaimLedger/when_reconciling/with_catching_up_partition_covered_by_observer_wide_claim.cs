// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_reconciling;

public class with_catching_up_partition_covered_by_observer_wide_claim : given.a_ledger
{
    void Establish()
    {
        _catchingUp.Add(_partition);
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, null, _resume);
    }

    void Because() => _divergences = _ledger.Reconcile(_replaying, _catchingUp, false, _resume);

    [Fact] void should_not_create_a_partition_claim() => _ledger.Snapshot().Claims.ShouldBeEmpty();
    [Fact] void should_keep_observer_wide_claim() => _ledger.Snapshot().Wide.Holder.ShouldEqual(ClaimHolderKind.ObserverCatchUp);
    [Fact] void should_not_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(1UL);
    [Fact] void should_not_report_a_release() => _divergences.ShouldBeEmpty();
}
