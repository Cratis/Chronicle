// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_observer_takes_pages_spanning_partitions : given.a_replay_observer_job
{
    IImmutableList<JobStepDetails> _steps;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed with { SubscriberType = typeof(IObserverPartitionPageSubscriber) });
    }

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_create_one_step() => _steps.Count.ShouldEqual(1);
    [Fact] void should_use_ordered_observer_step() => _steps[0].Type.ShouldEqual(typeof(IHandleEventsForObserver));
    [Fact] void should_not_enumerate_partitions() => _keyIndexes.DidNotReceive().GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>());
    [Fact] void should_request_replay_observation_state() => ((HandleEventsForObserverArguments)_steps[0].Request).EventObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] void should_remember_that_it_was_replayed_in_one_sweep() => _stateStorage.State.ReplayedInOneSweep.ShouldBeTrue();
}
