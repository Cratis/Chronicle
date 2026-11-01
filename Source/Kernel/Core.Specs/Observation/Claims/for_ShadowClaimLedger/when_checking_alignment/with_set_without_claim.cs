// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class with_set_without_claim : given.a_ledger
{
    bool _result;

    void Establish() => _catchingUp.Add(_partition);

    void Because() => _result = _ledger.IsAligned(_replaying, _catchingUp, false);

    [Fact] void should_not_be_aligned() => _result.ShouldBeFalse();
}
