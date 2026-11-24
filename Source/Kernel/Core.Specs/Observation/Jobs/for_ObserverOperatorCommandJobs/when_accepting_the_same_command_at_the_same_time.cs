// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandJobs;

public class when_accepting_the_same_command_at_the_same_time : Specification
{
    static readonly ObserverKey _key = new("one", "some-event-store", "some-namespace", EventSequenceId.Log);

    readonly JobId _started = JobId.New();
    readonly TaskCompletionSource _startCompletes = new();
    readonly List<JobState> _jobs = [];
    IJobsManager _jobsManager;
    JobId _first;
    JobId _second;

    void Establish()
    {
        _jobsManager = Substitute.For<IJobsManager>();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(_jobs.ToImmutableList()));
        _jobsManager.Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(Arg.Any<ObserverOperatorCommandRequest>())
            .Returns(async call =>
            {
                // A job being started is not listed until its start completes.
                await _startCompletes.Task;
                _jobs.Add(new JobState { Id = _started, Status = JobStatus.Running, Request = call.Arg<ObserverOperatorCommandRequest>() });
                return Result<JobId, StartJobError>.Success(_started);
            });
    }

    async Task Because()
    {
        var first = ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.RetryPartition, "some-partition");
        var second = ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.RetryPartition, "some-partition");
        await Task.Delay(200);
        _startCompletes.SetResult();
        _first = await first;
        _second = await second;
        await ObserverOperatorCommandJobs.Accept(_jobsManager, _key, ObserverOperatorCommand.RetryPartition, "another-partition");
    }

    [Fact] void should_start_one_job_for_the_two_commands() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Is<ObserverOperatorCommandRequest>(_ => _.Partition == "some-partition"));
    [Fact] void should_give_the_first_command_the_job() => _first.ShouldEqual(_started);
    [Fact] void should_let_the_second_command_join_the_job() => _second.ShouldEqual(_started);
    [Fact] void should_start_another_job_for_another_partition() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Is<ObserverOperatorCommandRequest>(_ => _.Partition == "another-partition"));
}
