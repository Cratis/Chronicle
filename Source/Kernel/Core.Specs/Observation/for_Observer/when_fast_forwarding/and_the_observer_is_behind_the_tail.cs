// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_fast_forwarding;

public class and_the_observer_is_behind_the_tail : given.an_observer
{
    FastForwardResult _result;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
    }

    async Task Because() => _result = await _observer.FastForward();

    [Fact] void should_report_fast_forwarded() => _result.Outcome.ShouldEqual(FastForwardOutcome.FastForwarded);
    [Fact] void should_report_where_it_was() => _result.PreviousNextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(5UL);
    [Fact] void should_report_where_it_is_now() => _result.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(101UL);
    [Fact] void should_expect_the_event_after_the_tail() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(101UL);
}
