// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_observer_is_caught_up_having_read_only_excluded_events : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable,
            NextEventSequenceNumber = EventSequenceNumber.First
        };
    }

    Task Because() => _observer.CaughtUp(EventSequenceNumber.Unavailable, 7UL);

    [Fact] void should_not_count_any_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_move_past_the_last_event_read() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)8UL);
}
