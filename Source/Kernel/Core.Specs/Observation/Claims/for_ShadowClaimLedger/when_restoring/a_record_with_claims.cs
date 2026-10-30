// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_restoring;

public class a_record_with_claims : given.a_ledger
{
    bool _legacy;
    ObserverWideClaim _wide;

    void Establish() => _wide = new(ClaimHolderKind.ObserverCatchUp, null, 7, _resume, []);

    void Because() => _ledger = ShadowClaimLedger.Restore(
        [new(_partition, ClaimHolderKind.PartitionReplay, null, 5, _resume, [])], _wide, 9, out _legacy);

    [Fact] void should_not_be_legacy() => _legacy.ShouldBeFalse();
    [Fact] void should_preserve_partition_epoch() => Claim().Epoch.Value.ShouldEqual(5UL);
    [Fact] void should_preserve_observer_wide_claim() => _ledger.Snapshot().Wide.ShouldEqual(_wide);
    [Fact] void should_preserve_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(9UL);
}
