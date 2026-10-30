// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_restoring;

public class a_legacy_record : given.a_ledger
{
    bool _legacy;

    void Because() => _ledger = ShadowClaimLedger.Restore(null, null, null, out _legacy);

    [Fact] void should_be_legacy() => _legacy.ShouldBeTrue();
    [Fact] void should_have_no_partition_claims() => _ledger.Snapshot().Claims.ShouldBeEmpty();
    [Fact] void should_have_no_observer_wide_claim() => _ledger.Snapshot().Wide.ShouldBeNull();
    [Fact] void should_start_at_revision_zero() => _ledger.Snapshot().Revision.Value.ShouldEqual(0UL);
}
