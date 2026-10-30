// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_handing_over;

public class a_held_claim : given.a_ledger
{
    void Establish() => GrantReplay();
    void Because() => _ledger.HandOver(_partition);

    [Fact] void should_remove_claim() => _ledger.Snapshot().Claims.ShouldBeEmpty();
    [Fact] void should_advance_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(2UL);
}
