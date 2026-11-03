// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class and_aligned_with_claims : given.a_ledger
{
    bool _result;
    ulong _allocated;
    ulong _revision;

    void Establish()
    {
        GrantReplay();
        _replaying.Add(_partition);
        _ledger.IsAligned(_replaying, _catchingUp, false);
        _revision = _ledger.Snapshot().Revision.Value;
    }

    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        _result = _ledger.IsAligned(_replaying, _catchingUp, false);
        _allocated = (ulong)(GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact] void should_be_aligned() => _result.ShouldBeTrue();
    [Fact] void should_not_allocate() => _allocated.ShouldEqual(0UL);
    [Fact] void should_have_nothing_to_compare() => _ledger.Compare(_replaying, _catchingUp, false).ShouldBeEmpty();
    [Fact] void should_have_nothing_to_reconcile() => _ledger.Reconcile(_replaying, _catchingUp, false, _resume).ShouldBeEmpty();
    [Fact] void should_not_change_the_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(_revision);
}
