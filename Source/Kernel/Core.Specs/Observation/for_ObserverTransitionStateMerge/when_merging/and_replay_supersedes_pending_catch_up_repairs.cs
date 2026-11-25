// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_replay_supersedes_pending_catch_up_repairs : given.a_transition_hook
{
    void Establish()
    {
        _handed = _handed with { PendingCatchUpRepairs = [new("B", 10UL, 13UL)] };
        _recorded = _handed;
        _returned = _handed with { IsReplaying = true, NextEventSequenceNumber = 0UL };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_discard_the_superseded_live_obligation() => _result.PendingCatchUpRepairs.ShouldBeEmpty();
}
