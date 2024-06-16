// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_leaving;

public class and_a_replay_job_is_already_running : given.a_replay_state
{
    async Task Establish()
    {
        stored_state = stored_state with
        {
            Type = ObserverType.Client
        };

        jobs_manager
            .Setup(_ => _.GetJobsOfType<IReplayObserver, ReplayObserverRequest>())
            .ReturnsAsync(new[]
                {
                    new JobState
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
                            }
                        ]
                    }
                }.ToImmutableList());

        stored_state = await state.OnEnter(stored_state);
    }

    async Task Because() => resulting_stored_state = await state.OnLeave(stored_state);

    [Fact] void should_not_end_replay() => observer_service_client.Verify(_ => _.BeginReplayFor(IsAny<ObserverDetails>()), Never);
}
