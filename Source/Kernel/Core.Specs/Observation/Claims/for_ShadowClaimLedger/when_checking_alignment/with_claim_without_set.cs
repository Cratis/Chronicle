// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class with_claim_without_set : given.a_ledger
{
    bool _result;

    void Establish() => GrantReplay();

    void Because() => _result = _ledger.IsAligned(_replaying, _catchingUp, false);

    [Fact] void should_not_be_aligned() => _result.ShouldBeFalse();
}
