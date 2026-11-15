// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_snapshotting;

public class after_handing_over_an_unclaimed_partition : given.a_ledger
{
    IReadOnlyList<Storage.Observation.PartitionClaim> _first;
    IReadOnlyList<Storage.Observation.PartitionClaim> _second;
    ulong _allocated;

    void Establish()
    {
        GrantReplay();
        _first = _ledger.Snapshot().Claims;
    }

    void Because()
    {
        _ledger.HandOver("unclaimed");
        var before = GC.GetAllocatedBytesForCurrentThread();
        _second = _ledger.Snapshot().Claims;
        _allocated = (ulong)(GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact] void should_return_the_same_claims() => _second.ShouldBeSame(_first);
    [Fact] void should_not_allocate_when_reading_the_snapshot() => _allocated.ShouldEqual(0UL);
}
