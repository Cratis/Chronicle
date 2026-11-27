// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class when_resuming_a_zero_step_handover_repair : given.a_partition_catch_up_job
{
    readonly ILogger<Job<CatchUpObserverPartitionRequest, JobStateWithLastHandledEvent>> _resumeLogger = Substitute.For<ILogger<Job<CatchUpObserverPartitionRequest, JobStateWithLastHandledEvent>>>();
    readonly TaskCompletionSource _acknowledgementStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _durableAcknowledgement = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedBeforeAcknowledgement;

    protected override async Task CreateJob()
    {
        var jobsStorage = Substitute.For<IJobsStorage>();
        jobsStorage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(_jobStorage, _jobStepStorage));
        _silo.AddService(jobsStorage);
        _resumeLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _silo.AddService(_resumeLogger);
        await base.CreateJob();
    }

    void Establish()
    {
        _jobStepStorage.GetForJob(Arg.Any<JobId>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));
        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));
        _partitionJob.RestoreForTesting(new()
        {
            Id = _jobId,
            Status = JobStatus.Stopped,
            StatusChanges = [new() { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow }],
            StepPlan = [],
            Request = new CatchUpObserverPartitionRequest(_request.ObserverKey, _request.ObserverType, "P", 11UL, []) { ToSequenceNumber = 13UL, IsHandoverRepair = true }
        });
        _observer.HandoverRepairCaughtUp(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<ulong?>())
            .Returns(_ =>
            {
                _acknowledgementStarted.TrySetResult();
                return _durableAcknowledgement.Task;
            });
    }

    async Task Because()
    {
        var resume = _partitionJob.Resume();
        try
        {
            await WaitForAcknowledgement(resume);
            _completedBeforeAcknowledgement = resume.IsCompleted;
        }
        finally
        {
            _durableAcknowledgement.TrySetResult();
            if (_acknowledgementStarted.Task.IsCompleted) await WaitForResumeCompletion(resume);
        }
    }

    async Task WaitForAcknowledgement(Task<Result<ResumeJobSuccess, ResumeJobError>> resume)
    {
        Task first;
        try
        {
            first = await Task.WhenAny(_acknowledgementStarted.Task, resume).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException($"Timed out waiting for HandoverRepairCaughtUp: {ResumeDiagnostics(resume)}", exception);
        }
        if (first == resume) throw await CompletedWithoutAcknowledgement(resume);
    }

    async Task<InvalidOperationException> CompletedWithoutAcknowledgement(Task<Result<ResumeJobSuccess, ResumeJobError>> resume)
    {
        try
        {
            var outcome = await resume;
            var value = outcome.Match(result => $"Result={result}", error => $"Error={error}; Value={error.Value}");
            return new($"Resume finished without HandoverRepairCaughtUp: {value}; {ResumeDiagnostics(resume)}");
        }
        catch (Exception exception)
        {
            return new($"Resume faulted without HandoverRepairCaughtUp: {ResumeDiagnostics(resume)}", exception);
        }
    }

    async Task WaitForResumeCompletion(Task resume)
    {
        try
        {
            await resume.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException($"Resume did not finish after durable acknowledgement: {ResumeDiagnostics(resume)}", exception);
        }
    }

    string ResumeDiagnostics(Task resume)
    {
        var exceptions = _resumeLogger.ReceivedCalls().SelectMany(_ => _.GetArguments().OfType<Exception>());
        return $"TaskStatus={resume.Status}; Exception={resume.Exception}; {_partitionJob.DescribeStateForTesting()}; LoggedExceptions={string.Join(Environment.NewLine, exceptions)}";
    }

    [Fact] void should_keep_completion_behind_the_durable_acknowledgement() => _completedBeforeAcknowledgement.ShouldBeFalse();
    [Fact] void should_acknowledge_the_entire_empty_bounded_range() => _observer.Received(1).HandoverRepairCaughtUp("P", 11UL, 14UL, Arg.Any<EventSequenceNumber>(), true, null);
    [Fact] void should_never_use_the_non_interleaving_partition_callback() => _observer.DidNotReceive().PartitionCaughtUp(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<ulong?>());
}
