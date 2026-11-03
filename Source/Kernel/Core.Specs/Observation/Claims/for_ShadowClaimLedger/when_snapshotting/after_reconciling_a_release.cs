// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_snapshotting;

public class after_reconciling_a_release : given.a_ledger
{
    IReadOnlyList<Storage.Observation.PartitionClaim> _claims;

    void Establish()
    {
        GrantReplay();
        _ledger.Snapshot();
    }

    void Because()
    {
        _ledger.Reconcile(_replaying, _catchingUp, false, _resume);
        _claims = _ledger.Snapshot().Claims;
    }

    [Fact] void should_exclude_the_claim() => _claims.ShouldBeEmpty();
}
