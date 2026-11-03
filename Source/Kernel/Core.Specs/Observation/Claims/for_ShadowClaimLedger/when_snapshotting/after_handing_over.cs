// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_snapshotting;

public class after_handing_over : given.a_ledger
{
    IReadOnlyList<Storage.Observation.PartitionClaim> _claims;

    void Establish()
    {
        GrantReplay();
        _ledger.Snapshot();
    }

    void Because()
    {
        _ledger.HandOver(_partition);
        _claims = _ledger.Snapshot().Claims;
    }

    [Fact] void should_exclude_the_claim() => _claims.ShouldBeEmpty();
}
