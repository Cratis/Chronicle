// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_replay_was_already_running : given.a_transition_hook
{
    void Establish()
    {
        _handed = _handed with { IsReplaying = true };
        _recorded = _handed with { NextEventSequenceNumber = 43UL, LastHandledEventSequenceNumber = 42UL, TailEventSequenceNumber = 42UL };
        _returned = _handed;
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_keep_the_last_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_tail_position() => _result.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
