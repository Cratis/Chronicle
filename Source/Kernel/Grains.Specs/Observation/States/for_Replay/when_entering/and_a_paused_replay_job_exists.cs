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
    ObserverDetails _observerDetails;

    void Establish()
    {
        _storedState = _storedState with
        {
            Type = ObserverType.Client
        };

        _pausedJob = new()
        {
            Id = JobId.New(),
            Request = new ReplayObserverRequest(
                            _observerKey,
                            _subscription,
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
                    Status = JobStatus.Paused,
                    Occurred = DateTimeOffset.UtcNow
                }
            ],
            Status = JobStatus.Paused
        };

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(new[]
                {
                    _pausedJob
                }.ToImmutableList());

        _observerServiceClient
            .When(_ => _.BeginReplayFor(Arg.Any<ObserverDetails>()))
            .Do(callInfo => _observerDetails = callInfo.Arg<ObserverDetails>());
        }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_resume_paused_job() => _jobsManager.Received(1).Resume(_pausedJob.Id);
    [Fact] void should_begin_replay_only_one() => _observerServiceClient.Received(1).BeginReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_begin_replay_for_correct_observer() => _observerDetails.ShouldEqual(new(_storedState.Id, _observerKey, ObserverType.Client));
}
