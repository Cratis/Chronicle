// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reactivating;

public class and_unsubscribed_while_replaying : given.an_unsubscribed_replaying_observer
{
    Task Because() => Reactivate();

    [Fact] async Task should_be_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_preserve_the_defined_event_types() => _definitionStorage.State.EventTypes.ShouldEqual(_definedEventTypes);
    [Fact] void should_keep_the_pending_replay() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_not_start_a_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_not_reset_handled_counts() => _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
}
