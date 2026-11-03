// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class with_catching_up_partitions_covered_by_observer_wide_catch_up : given.a_ledger
{
    bool _result;

    void Establish()
    {
        _ledger.GrantObserverWide(ClaimHolderKind.ObserverCatchUp, null, _resume);
        _catchingUp.Add(_partition);
    }

    void Because() => _result = _ledger.IsAligned(_replaying, _catchingUp, false);

    [Fact] void should_be_aligned() => _result.ShouldBeTrue();
}
