// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_next_event_lookup_fails : given.an_observer_with_client_owned_subscription
{
    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)20UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
    }

    Task Because() => _observer.RunWatchdogAsync();

    [Fact] void should_leave_the_position_unchanged() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_not_persist_progress() => _storageStats.Writes.ShouldEqual(0);
}
