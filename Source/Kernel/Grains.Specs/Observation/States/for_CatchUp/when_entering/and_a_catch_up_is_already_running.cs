// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_CatchUp.when_entering;

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
                            [new EventType(Guid.NewGuid().ToString(), EventGeneration.First)]),
                        StatusChanges =
                        [
                            new JobStatusChanged
                            {
                                Status = JobStatus.Running,
                                Occurred = DateTimeOffset.UtcNow
                            }
                        ]
                    }
                }.ToImmutableList());
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_not_resume_a_job() => jobs_manager.Verify(_ => _.Resume(IsAny<JobId>()), Never);
    [Fact] void should_not_start_a_new_job() => jobs_manager.Verify(_ => _.Start<ICatchUpObserver, CatchUpObserverRequest>(IsAny<JobId>(), IsAny<CatchUpObserverRequest>()), Never);
}
