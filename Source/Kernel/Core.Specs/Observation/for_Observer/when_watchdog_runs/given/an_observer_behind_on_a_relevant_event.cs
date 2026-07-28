// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

public class an_observer_behind_on_a_relevant_event : an_observer_with_client_owned_subscription
{
    protected EventSequenceNumber _nextSequenceNumber;
    protected EventSequenceNumber _tailSequenceNumber;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _nextSequenceNumber = 5L;
        _tailSequenceNumber = 20L;

        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = _nextSequenceNumber };

        _eventSequence.GetTailSequenceNumber().Returns(_tailSequenceNumber);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_nextSequenceNumber, Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)10L);

        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    protected void ShouldNotHaveResubscribed() => _appendedEventsQueues
        .DidNotReceive()
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());

    protected void ShouldNotHaveStartedCatchup() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
