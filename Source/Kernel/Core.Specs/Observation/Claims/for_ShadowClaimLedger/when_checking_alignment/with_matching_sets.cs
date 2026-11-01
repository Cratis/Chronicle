// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class with_matching_sets : given.a_ledger
{
    bool _result;

    void Establish()
    {
        GrantReplay();
        _replaying.Add(_partition);
    }

    void Because() => _result = _ledger.IsAligned(_replaying, _catchingUp, false);

    [Fact] void should_be_aligned() => _result.ShouldBeTrue();
}
