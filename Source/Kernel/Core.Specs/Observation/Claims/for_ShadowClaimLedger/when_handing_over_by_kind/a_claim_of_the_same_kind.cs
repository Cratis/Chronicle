// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over_by_kind;

public class a_claim_of_the_same_kind : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => _ledger.HandOver(_partition, ClaimHolderKind.PartitionReplay);

    [Fact] void should_release_the_claim() => _ledger.ClaimFor(_partition).ShouldBeNull();
}
