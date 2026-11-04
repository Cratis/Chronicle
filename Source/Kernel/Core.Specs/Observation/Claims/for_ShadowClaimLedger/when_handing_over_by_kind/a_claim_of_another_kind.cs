// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over_by_kind;

public class a_claim_of_another_kind : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => _ledger.HandOver(_partition, ClaimHolderKind.PartitionCatchUp);

    [Fact] void should_keep_the_claim() => Claim().Holder.ShouldEqual(ClaimHolderKind.PartitionReplay);
}
