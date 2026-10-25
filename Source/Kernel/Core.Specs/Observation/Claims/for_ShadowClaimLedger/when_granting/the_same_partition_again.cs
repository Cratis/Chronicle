// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_granting;

public class the_same_partition_again : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => GrantReplay();

    [Fact] void should_issue_a_fresh_epoch() => Claim().Epoch.Value.ShouldEqual(2UL);
    [Fact] void should_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(2UL);
    [Fact] void should_keep_one_claim() => _ledger.Snapshot().Claims.Count.ShouldEqual(1);
}
