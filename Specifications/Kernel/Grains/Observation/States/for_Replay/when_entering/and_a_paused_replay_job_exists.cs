// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_entering;

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
                            [new EventType(Guid.NewGuid(), EventGeneration.First)]),
            StatusChanges =
            [
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
            ]
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
