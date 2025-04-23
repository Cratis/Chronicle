// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_entering;

public class and_a_paused_replay_job_exists : given.a_replay_state
{
    JobState _pausedJob;

    void Establish()
    {
        _storedState = _storedState with
        {
            Type = ObserverType.Reactor
        };

        _pausedJob = new()
        {
            Id = JobId.New(),
            Request = new ReplayObserverRequest(
                            _observerKey,
                            ObserverType.Unknown,
                            [new(Guid.NewGuid().ToString(), EventTypeGeneration.First)]),
            StatusChanges =
            [
                new()
                {
                    Status = JobStatus.Running,
                    Occurred = DateTimeOffset.UtcNow
                },
                new()
                {
                    Status = JobStatus.Stopped,
                    Occurred = DateTimeOffset.UtcNow
                }
            ],
            Status = JobStatus.Stopped
        };

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(new[]
                {
                    _pausedJob
                }.ToImmutableList());
        }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_resume_paused_job() => _jobsManager.Received(1).Resume(_pausedJob.Id);
}
