// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_entering;

public class and_no_jobs_are_running : given.a_replay_state
{
    ReplayObserverRequest _request;
    ObserverDetails _observerDetails;

    void Establish()
    {
        _storedState = _storedState with
        {
            Type = ObserverType.Reactor,
            NextEventSequenceNumber = 42,
            EventTypes =
            [
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventTypeGeneration.First),
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventTypeGeneration.First)
            ]
        };
        _subscription = _subscription with
        {
            EventTypes =
            [
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventTypeGeneration.First),
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventTypeGeneration.First)
            ]
        };

        _jobsManager
            .When(_ => _.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<JobId>(), Arg.Any<ReplayObserverRequest>()))
            .Do(callInfo => _request = callInfo.Arg<ReplayObserverRequest>());

        _observerServiceClient
            .When(_ => _.BeginReplayFor(Arg.Any<ObserverDetails>()))
            .Do(callInfo => _observerDetails = callInfo.Arg<ObserverDetails>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_catch_up_job() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<JobId>(), Arg.Any<ReplayObserverRequest>());
    [Fact] void should_start_catch_up_job_with_correct_observer_id() => _request.ObserverKey.ObserverId.ShouldEqual(_storedState.Id);
    [Fact] void should_start_catch_up_job_with_correct_observer_key() => _request.ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_start_catch_up_job_with_correct_subscription() => _request.ObserverSubscription.ShouldEqual(_subscription);
    [Fact] void should_start_catch_up_job_with_correct_event_types() => _request.EventTypes.ShouldEqual(_storedState.EventTypes);
    [Fact] void should_begin_replay_only_one() => _observerServiceClient.Received(1).BeginReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_begin_replay_for_correct_observer() => _observerDetails.ShouldEqual(new ObserverDetails(_storedState.Id, _observerKey, ObserverType.Reactor));
}
