// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_granting_observer_wide;

public class without_partition_claims : given.a_ledger
{
    void Because() => _divergences = _ledger.GrantObserverWide(ClaimHolderKind.ObserverReplay, null, _resume);

    [Fact] void should_report_no_divergence() => _divergences.ShouldBeEmpty();
    [Fact] void should_record_observer_wide_claim() => _ledger.Snapshot().Wide.Holder.ShouldEqual(ClaimHolderKind.ObserverReplay);
}
