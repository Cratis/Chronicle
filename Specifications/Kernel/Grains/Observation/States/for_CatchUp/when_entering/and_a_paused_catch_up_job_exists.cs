// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Storage.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_CatchUp.when_entering;

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
            .Setup(_ => _.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>())
            .ReturnsAsync(new[]
                {
                    paused_job
                }.ToImmutableList());
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_resume_paused_job() => jobs_manager.Verify(_ => _.Resume(paused_job.Id), Once);
}
