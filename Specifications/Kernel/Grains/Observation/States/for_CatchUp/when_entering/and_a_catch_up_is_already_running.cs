// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Storage.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_CatchUp.when_entering;

public class and_a_catch_up_is_already_running : given.a_catch_up_state
{
    void Establish()
    {
        jobs_manager
            .Setup(_ => _.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>())
            .ReturnsAsync(new[]
                {
                    new JobState
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
