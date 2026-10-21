// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_the_observer_is_quarantined : given.an_observer_with_a_superseded_client
{
    ObserverSubscription _subscription;

    async Task Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        await _observer.TransitionTo<QuarantinedObserver>();
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_activeClient.ConnectionId);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_hand_over_to_the_superseded_client() => _subscription.EventTypes.ShouldContainOnly(superseded_event_type);
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_start_catchup() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_start_replay() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
