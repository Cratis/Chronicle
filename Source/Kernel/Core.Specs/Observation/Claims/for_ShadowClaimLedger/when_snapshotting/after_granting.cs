// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_snapshotting;

public class after_granting : given.a_ledger
{
    IReadOnlyList<Storage.Observation.PartitionClaim> _claims;

    void Establish() => _ledger.Snapshot();

    void Because()
    {
        GrantReplay();
        _claims = _ledger.Snapshot().Claims;
    }

    [Fact] void should_include_the_claim() => _claims.Single().Partition.ShouldEqual(_partition);
}
