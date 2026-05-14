// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_next_sequence_number_is_behind_with_no_relevant_events : given.an_observer_with_client_owned_subscription
{
    static EventSequenceNumber _nextSequenceNumber;
    static EventSequenceNumber _tailSequenceNumber;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _nextSequenceNumber = 5L;
        _tailSequenceNumber = 20L;

        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = _nextSequenceNumber };

        _eventSequence.GetTailSequenceNumber().Returns(_tailSequenceNumber);

        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_nextSequenceNumber, Arg.Any<IEnumerable<EventType>>())
            .Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_fast_forward_next_event_sequence_number() =>
        _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_tailSequenceNumber.Next());

    [Fact] void should_write_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
