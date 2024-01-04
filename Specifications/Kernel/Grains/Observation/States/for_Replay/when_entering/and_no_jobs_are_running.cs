// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Replay.when_entering;

public class and_no_jobs_are_running : given.a_replay_state
{
    ReplayObserverRequest request;
    ObserverDetails observer_details;

    void Establish()
    {
        stored_state = stored_state with
        {
            Type = ObserverType.Client,
            Handled = 41,
            NextEventSequenceNumber = 42,
            EventTypes = new[]
            {
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventGeneration.First),
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventGeneration.First)
            }
        };
        subscription = subscription with
        {
            EventTypes = new[]
            {
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventGeneration.First),
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventGeneration.First)
            }
        };

        jobs_manager
            .Setup(_ => _.Start<IReplayObserver, ReplayObserverRequest>(IsAny<JobId>(), IsAny<ReplayObserverRequest>()))
            .Callback<JobId, ReplayObserverRequest>((_, requestAtStart) => request = requestAtStart);

        observer_service_client
            .Setup(_ => _.BeginReplayFor(IsAny<ObserverDetails>()))
            .Callback((ObserverDetails observer) => observer_details = observer);
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_reset_handled_count() => resulting_stored_state.ShouldEqual(stored_state with { Handled = EventCount.Zero });
    [Fact] void should_start_catch_up_job() => jobs_manager.Verify(_ => _.Start<IReplayObserver, ReplayObserverRequest>(IsAny<JobId>(), IsAny<ReplayObserverRequest>()), Once);
    [Fact] void should_start_catch_up_job_with_correct_observer_id() => request.ObserverId.ShouldEqual(stored_state.ObserverId);
    [Fact] void should_start_catch_up_job_with_correct_observer_key() => request.ObserverKey.ShouldEqual(observer_key);
    [Fact] void should_start_catch_up_job_with_correct_subscription() => request.ObserverSubscription.ShouldEqual(subscription);
    [Fact] void should_start_catch_up_job_with_correct_event_types() => request.EventTypes.ShouldEqual(stored_state.EventTypes);
    [Fact] void should_begin_replay_only_one() => observer_service_client.Verify(_ => _.BeginReplayFor(IsAny<ObserverDetails>()), Once);
    [Fact] void should_begin_replay_for_correct_observer() => observer_details.ShouldEqual(new ObserverDetails(stored_state.ObserverId, observer_key, ObserverType.Client));
}
