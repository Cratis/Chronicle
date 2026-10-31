// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over;

public class an_unknown_partition : given.a_ledger
{
    void Because() => _ledger.HandOver(_partition);

    [Fact] void should_not_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(0UL);
    [Fact] void should_keep_claims_empty() => _ledger.Snapshot().Claims.ShouldBeEmpty();
}
