// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_CatchUp.when_entering;

public class and_no_jobs_are_running : given.a_catch_up_state
{
    CatchUpObserverRequest request;

    void Establish()
    {
        stored_state = stored_state with
        {
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
            .Setup(_ => _.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>())
            .ReturnsAsync(Enumerable.Empty<JobState<CatchUpObserverRequest>>().ToImmutableList());

        jobs_manager
            .Setup(_ => _.Start<ICatchUpObserver, CatchUpObserverRequest>(IsAny<JobId>(), IsAny<CatchUpObserverRequest>()))
            .Callback<JobId, CatchUpObserverRequest>((_, requestAtStart) => request = requestAtStart);
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_return_same_state() => resulting_stored_state.ShouldBeSame(stored_state);
    [Fact] void should_start_catch_up_job() => jobs_manager.Verify(_ => _.Start<ICatchUpObserver, CatchUpObserverRequest>(IsAny<JobId>(), IsAny<CatchUpObserverRequest>()), Once);
    [Fact] void should_start_catch_up_job_with_correct_observer_id() => request.ObserverId.ShouldEqual(stored_state.ObserverId);
    [Fact] void should_start_catch_up_job_with_correct_observer_key() => request.ObserverKey.ShouldEqual(observer_key);
    [Fact] void should_start_catch_up_job_with_correct_subscription() => request.ObserverSubscription.ShouldEqual(subscription);
    [Fact] void should_start_catch_up_job_with_correct_event_sequence_number() => request.FromEventSequenceNumber.ShouldEqual(stored_state.NextEventSequenceNumber);
    [Fact] void should_start_catch_up_job_with_correct_event_types() => request.EventTypes.ShouldEqual(stored_state.EventTypes);
}

public class and_a_catch_up_is_already_running : given.a_catch_up_state
{
    void Establish()
    {
        jobs_manager
            .Setup(_ => _.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>())
            .ReturnsAsync(new[]
                {
                    new JobState<CatchUpObserverRequest>
                    {
                        Id = JobId.New(),
                        Request = new CatchUpObserverRequest(
                            stored_state.ObserverId,
                            observer_key,
                            subscription,
                            42,
                            new[] { new EventType(Guid.NewGuid(), EventGeneration.First) }),
                        StatusChanges = new[]
                        {
                            new JobStatusChanged
                            {
                                Status = JobStatus.Running,
                                Occurred = DateTimeOffset.UtcNow
                            }
                        }.ToList()
                    }
                }.ToImmutableList());
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_not_resume_a_job() => jobs_manager.Verify(_ => _.Resume(IsAny<JobId>()), Never);
    [Fact] void should_not_start_a_new_job() => jobs_manager.Verify(_ => _.Start<ICatchUpObserver, CatchUpObserverRequest>(IsAny<JobId>(), IsAny<CatchUpObserverRequest>()), Never);
}
