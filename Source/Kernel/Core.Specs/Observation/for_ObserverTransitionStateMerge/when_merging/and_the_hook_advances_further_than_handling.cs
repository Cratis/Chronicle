// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_the_hook_advances_further_than_handling : given.a_transition_hook
{
    void Establish()
    {
        _recorded = _handed with { NextEventSequenceNumber = 2UL, LastHandledEventSequenceNumber = 1UL, TailEventSequenceNumber = 1UL };
        _returned = _handed with { NextEventSequenceNumber = 43UL, LastHandledEventSequenceNumber = 42UL, TailEventSequenceNumber = 42UL };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_keep_the_last_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_tail_position() => _result.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
