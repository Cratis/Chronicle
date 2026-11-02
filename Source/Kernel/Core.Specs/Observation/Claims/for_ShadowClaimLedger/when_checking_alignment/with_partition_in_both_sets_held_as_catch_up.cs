// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.when_checking_alignment;

public class with_partition_in_both_sets_held_as_catch_up : given.a_ledger
{
    bool _result;

    void Establish()
    {
        _ledger.Grant(_partition, ClaimHolderKind.PartitionCatchUp, null, _resume);
        _replaying.Add(_partition);
        _catchingUp.Add(_partition);
    }

    void Because() => _result = _ledger.IsAligned(_replaying, _catchingUp, false);

    [Fact] void should_not_be_aligned() => _result.ShouldBeFalse();
}
