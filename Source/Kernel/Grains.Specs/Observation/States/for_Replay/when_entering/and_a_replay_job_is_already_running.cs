// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_entering;

public class and_a_replay_job_is_already_running : given.a_replay_state
{
    void Establish()
    {
        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(new[]
                {
                    new JobState
                    {
                        Id = JobId.New(),
                        Request = new ReplayObserverRequest(
                            _observerKey,
                            ObserverType.Unknown,
                            [new EventType(Guid.NewGuid().ToString(), EventTypeGeneration.First)]),
                        StatusChanges =
                        [
                            new JobStatusChanged
                            {
                                Status = JobStatus.Running,
                                Occurred = DateTimeOffset.UtcNow
                            }
                        ],
                        Status = JobStatus.Running
                    }
                }.ToImmutableList());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_resume_a_job() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_start_a_new_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
