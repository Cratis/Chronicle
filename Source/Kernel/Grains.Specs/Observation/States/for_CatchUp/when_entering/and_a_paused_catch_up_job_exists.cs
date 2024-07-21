// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_CatchUp.when_entering;

public class and_a_paused_catch_up_job_exists : given.a_catch_up_state
{
    JobState paused_job;
    void Establish()
    {
        paused_job = new JobState
        {
            Id = JobId.New(),
            Request = new CatchUpObserverRequest(
                            stored_state.ObserverId,
                            observer_key,
                            subscription,
                            42,
                            [new EventType(Guid.NewGuid().ToString(), EventGeneration.First)]),
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
            .Setup(_ => _.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>())
            .ReturnsAsync(new[]
                {
                    paused_job
                }.ToImmutableList());
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_resume_paused_job() => jobs_manager.Verify(_ => _.Resume(paused_job.Id), Once);
}
