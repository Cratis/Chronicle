// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_restoring;

public class a_record_with_claims_and_granting_a_replay : given.a_ledger
{
    void Because()
    {
        _ledger = ShadowClaimLedger.Restore(
            [new(_partition, ClaimHolderKind.PartitionReplay, null, 5, _resume, [])],
            new(ClaimHolderKind.ObserverCatchUp, null, 7, _resume, []),
            9,
            out _);
        GrantReplay();
    }

    [Fact] void should_allocate_above_the_stored_epochs() => Claim().Epoch.Value.ShouldEqual(10UL);
}
