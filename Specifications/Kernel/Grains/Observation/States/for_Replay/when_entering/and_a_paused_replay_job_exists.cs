// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Storage.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Replay.when_entering;

public class and_a_paused_replay_job_exists : given.a_replay_state
{
    JobState paused_job;
    ObserverDetails observer_details;

    void Establish()
    {
        stored_state = stored_state with
        {
            Type = ObserverType.Client
        };

        paused_job = new JobState
        {
            Id = JobId.New(),
            Request = new ReplayObserverRequest(
                            stored_state.ObserverId,
                            observer_key,
                            subscription,
                            new[] { new EventType(Guid.NewGuid(), EventGeneration.First) }),
            StatusChanges = new[]
            {
                new JobStatusChanged
                {
                    Status = JobStatus.Running,
                    Occurred = DateTimeOffset.UtcNow
                },
                new JobStatusChanged
                {
                    Status = JobStatus.Paused,
                    Occurred = DateTimeOffset.UtcNow
                }
            }.ToList()
        };

        jobs_manager
            .Setup(_ => _.GetJobsOfType<IReplayObserver, ReplayObserverRequest>())
            .ReturnsAsync(new[]
                {
                    paused_job
                }.ToImmutableList());

        observer_service_client
            .Setup(_ => _.BeginReplayFor(IsAny<ObserverDetails>()))
            .Callback((ObserverDetails observer) => observer_details = observer);
        }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_resume_paused_job() => jobs_manager.Verify(_ => _.Resume(paused_job.Id), Once);
    [Fact] void should_begin_replay_only_one() => observer_service_client.Verify(_ => _.BeginReplayFor(IsAny<ObserverDetails>()), Once);
    [Fact] void should_begin_replay_for_correct_observer() => observer_details.ShouldEqual(new ObserverDetails(stored_state.ObserverId, observer_key, ObserverType.Client));
}
