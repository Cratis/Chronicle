// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States.for_CatchUp.when_entering;

public class and_a_paused_catch_up_job_exists : given.a_catch_up_state
{
    JobState _pausedJob;

    void Establish()
    {
        _pausedJob = new JobState
        {
            Id = JobId.New(),
            Request = new CatchUpObserverRequest(
                            _observerKey,
                            _subscription,
                            42,
                            [new EventType(Guid.NewGuid().ToString(), EventTypeGeneration.First)]),
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
            ],
            Status = JobStatus.Paused
        };

        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(new[]
                {
                    _pausedJob
                }.ToImmutableList());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_resume_paused_job() => _jobsManager.Received(1).Resume(_pausedJob.Id);
}
