// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_a_repair_is_completed_during_the_hook : given.a_transition_hook
{
    void Establish()
    {
        _handed = _handed with { PendingCatchUpRepairs = [new("B", 10UL, 13UL)] };
        _recorded = _handed with { PendingCatchUpRepairs = [] };
        _returned = _handed;
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_not_resurrect_the_old_repair() => _result.PendingCatchUpRepairs.ShouldBeEmpty();
}
