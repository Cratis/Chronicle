// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_retired_replaying_projection_was_reactivated : given.a_replaying_projection
{
    Guid _retiredLifecycle;

    async Task Establish()
    {
        await _observer.Retire();
        await Crash();
        _retiredLifecycle = _stateStorage.State.AlertLifecycleId;
        _jobsManager.ClearReceivedCalls();
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Subscribe<ObserverSubscriber>(ObserverType.Projection, [], SiloAddress.Zero);

    [Fact] void should_begin_a_new_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_retiredLifecycle);
    [Fact] void should_restore_active_disposition() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] async Task should_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] async Task should_resume_the_replay_job() => await _jobsManager.Received(1).Resume(_replayJob);
    [Fact] async Task should_reset_counts_once() => await _observerHandledCountsStorage.Received(1).RemoveAllFor(Arg.Any<ObserverId>());
}
