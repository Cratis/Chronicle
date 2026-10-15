// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_starting_at_tail;

public class and_the_observer_already_exists : given.an_observer_with_reloadable_state
{
    bool _result;

    void Establish()
    {
        _reloadableStateStorage.PersistedState = _reloadableStateStorage.PersistedState with
        {
            Identifier = _observerId,
            NextEventSequenceNumber = 5UL
        };
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because() => _result = await _observer.StartAtTailWhenNew();

    [Fact] void should_report_that_it_was_not_positioned() => _result.ShouldBeFalse();
    [Fact] void should_keep_its_position() => _reloadableStateStorage.PersistedState.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(5UL);
    [Fact] void should_not_even_read_the_tail() => _eventSequence.DidNotReceive().GetTailSequenceNumber();
}
