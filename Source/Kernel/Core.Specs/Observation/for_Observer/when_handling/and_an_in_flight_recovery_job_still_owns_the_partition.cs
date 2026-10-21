// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// An in-flight recovery job found at activation owns its partition even after routing has cleared the markers that
/// said so, so another partition's retry moving the position past its queued event is not suspected.
/// </summary>
public class and_an_in_flight_recovery_job_still_owns_the_partition : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 102UL,
            InFlightPartitions = new HashSet<Key> { "Q" }
        };
        await Crash();
        _observer.SetSubscription(subscription);
        await _observer.TransitionTo<Routing>();
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.FailedPartitionRecovered("P", 103UL);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
    }

    [Fact] void should_have_advanced_past_the_queued_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)104UL);
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
}
