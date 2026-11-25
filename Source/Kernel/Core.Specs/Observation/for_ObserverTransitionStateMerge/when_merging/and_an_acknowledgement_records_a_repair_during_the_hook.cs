// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_an_acknowledgement_records_a_repair_during_the_hook : given.a_transition_hook
{
    void Establish()
    {
        _recorded = _handed with { NextEventSequenceNumber = 13UL, PendingCatchUpRepairs = [new("B", 10UL, 13UL)] };
        _returned = _handed with { NextEventSequenceNumber = 9UL };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_not_lose_the_repair_when_the_hook_returns_its_old_state() => _result.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
}
