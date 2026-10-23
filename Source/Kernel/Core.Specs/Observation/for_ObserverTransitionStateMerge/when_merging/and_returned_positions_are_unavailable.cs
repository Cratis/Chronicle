// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_returned_positions_are_unavailable : given.a_transition_hook
{
    void Establish()
    {
        _handed = _handed with { LastHandledEventSequenceNumber = 5UL, TailEventSequenceNumber = 5UL };
        _recorded = _handed with { LastHandledEventSequenceNumber = 42UL, TailEventSequenceNumber = 42UL };
        _returned = _handed with { LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable, TailEventSequenceNumber = EventSequenceNumber.Unavailable };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_keep_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)0UL);
    [Fact] void should_keep_the_last_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_tail_position() => _result.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
