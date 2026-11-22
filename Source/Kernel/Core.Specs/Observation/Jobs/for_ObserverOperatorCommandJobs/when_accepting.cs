// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandJobs;

public class when_accepting : Specification
{
    static readonly ObserverKey _key = new("one", "some-event-store", "some-namespace", EventSequenceId.Log);
    static readonly ObserverKey _other = new("other", "some-event-store", "some-namespace", EventSequenceId.Log);

    readonly JobId _pending = JobId.New();
    readonly JobId _stopped = JobId.New();
    readonly JobId _new = JobId.New();
    IJobsManager _jobsManager;
    List<JobState> _jobs;
    JobId _pendingReplay;
    JobId _repeatedReplay;
    JobId _retry;
    JobId _replayOfAnotherObserver;
    JobId _afterStop;

    void Establish()
    {
        _jobs = [];
        _jobsManager = Substitute.For<IJobsManager>();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(_jobs.ToImmutableList()));
        _jobsManager.Resume(_stopped).Returns(true);
        _jobsManager.Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(Arg.Any<ObserverOperatorCommandRequest>())
            .Returns(_ => Task.FromResult(Result<JobId, StartJobError>.Success(_new)));
    }

    async Task Because()
    {
        _pendingReplay = await ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.Replay);
        _jobs.Add(new JobState { Id = _pending, Status = JobStatus.Running, Request = new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.Replay) });
        _repeatedReplay = await ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.Replay);
        _retry = await ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.RetryFailedPartitions);
        _replayOfAnotherObserver = await ObserverOperatorCommandJobs.Accept(_jobsManager, _other, ObserverOperatorCommand.Replay);

        _jobs.Clear();
        _jobs.Add(new JobState { Id = _stopped, Status = JobStatus.Stopped, Request = new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.Replay) });
        _afterStop = await ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.Replay);
    }

    [Fact] void should_start_a_job_when_there_is_none() => _pendingReplay.ShouldEqual(_new);
    [Fact] void should_join_the_job_that_is_pending_for_the_same_command() => _repeatedReplay.ShouldEqual(_pending);
    [Fact] void should_not_join_a_job_for_another_command() => _retry.ShouldEqual(_new);
    [Fact] void should_not_join_a_job_for_another_observer() => _replayOfAnotherObserver.ShouldEqual(_new);
    [Fact] void should_resume_a_stopped_job_for_the_same_command() => _afterStop.ShouldEqual(_stopped);
    [Fact] void should_start_a_job_exactly_when_none_could_be_joined() => _jobsManager.Received(3).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
