// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Being behind is normal - a live delivery in flight looks exactly like a stranded observer from the state alone.
/// As long as the queue still holds the subscription, the events are on their way and the watchdog leaves the
/// observer alone rather than tearing down a healthy subscription on every tick.
/// </summary>
public class and_the_queue_still_holds_the_subscription : given.an_observer_behind_on_a_relevant_event
{
    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();

    [Fact] void should_not_start_a_catchup_job() => ShouldNotHaveStartedCatchup();

    [Fact] void should_not_fast_forward_next_event_sequence_number() =>
        _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
}
