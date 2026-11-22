// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

/// <summary>
/// Repeating the command while its job is unfinished joins that job instead of starting another.
/// </summary>
public class and_a_replay_job_is_already_running : given.a_replayable_observer
{
    readonly JobId _jobId = JobId.New();
    ReplayResponse _response;

    void Establish() => _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(
        ImmutableList.Create(new JobState
        {
            Id = _jobId,
            Status = JobStatus.Running,
            Request = new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.Replay)
        })));

    async Task Because() => _response = await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "inbox-source"
    });

    [Fact] void should_return_the_existing_job_id() => _response.JobId.ShouldEqual(_jobId.Value.ToString());
    [Fact] void should_not_start_another_job() => _jobsManager.DidNotReceive().Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
