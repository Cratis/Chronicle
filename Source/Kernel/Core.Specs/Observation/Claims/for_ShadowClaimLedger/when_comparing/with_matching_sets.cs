// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_comparing;

public class with_matching_sets : given.a_ledger
{
    void Establish()
    {
        GrantReplay();
        _replaying.Add(_partition);
    }

    void Because() => _divergences = _ledger.Compare(_replaying, _catchingUp, false);

    [Fact] void should_have_no_divergence() => _divergences.ShouldBeEmpty();
    [Fact] void should_not_mutate_revision() => _ledger.Snapshot().Revision.Value.ShouldEqual(1UL);
}
