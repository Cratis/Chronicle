// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Catch-up read events after the last one it handled, excluded by the observer's filters. The observer moves past
/// them, so routing does not see them as unhandled and start the same catch-up again, without counting them as handled.
/// </summary>
public class when_observer_is_caught_up_having_read_past_the_last_handled_event : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 1UL,
            NextEventSequenceNumber = 2UL
        };
    }

    Task Because() => _observer.CaughtUp(JobId.New(), 3UL, 7UL);

    [Fact] void should_count_only_the_handled_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_move_past_the_last_event_read() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)8UL);
}
