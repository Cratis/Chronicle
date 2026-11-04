// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_snapshotting;

public class with_claims_out_of_partition_order : given.a_ledger
{
    IReadOnlyList<PartitionClaim> _claims;

    void Establish()
    {
        _ledger.Grant("b", ClaimHolderKind.PartitionReplay, null, _resume);
        _ledger.Grant("a", ClaimHolderKind.PartitionReplay, null, _resume);
    }

    void Because() => _claims = _ledger.Snapshot().Claims;

    [Fact] void should_order_by_partition() => _claims.Select(_ => _.Partition.ToString()).ShouldEqual(["a", "b"]);
}
