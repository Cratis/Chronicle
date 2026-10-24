// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_granting_observer_wide;

public class while_partition_claims_are_held : given.a_ledger
{
    void Establish()
    {
        GrantReplay();
        _ledger.Grant("failure", ClaimHolderKind.FailureRecovery, null, _resume);
    }

    void Because() => _divergences = _ledger.GrantObserverWide(ClaimHolderKind.ObserverReplay, null, _resume);

    [Fact] void should_report_every_partition_including_failure_recovery() => _divergences.Count.ShouldEqual(2);
    [Fact] void should_report_exclusion_for_each_claim() => _divergences.All(_ => _.Divergence == ShadowClaimDivergence.ExclusionViolated).ShouldBeTrue();
    [Fact] void should_record_observer_wide_claim() => _ledger.Snapshot().Wide.Holder.ShouldEqual(ClaimHolderKind.ObserverReplay);
    [Fact] void should_allocate_the_next_epoch() => _ledger.Snapshot().Wide.Epoch.Value.ShouldEqual(3UL);
    [Fact] void should_have_no_pending_work() => _ledger.Snapshot().Wide.PendingWork.ShouldBeEmpty();
}
