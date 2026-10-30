// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.given;

/// <summary>
/// A started reactor replay whose steps remain running until the specifications report their outcomes.
/// Retries are due at once. Step identities are captured during preparation, including fresh partitions.
/// </summary>
public class a_running_reactor_replay : a_replay_observer_job
{
    readonly Dictionary<Guid, IHandleEventsForPartition> _stepsById = [];
    readonly List<Guid> _stepIds = [];

    protected virtual int NumberOfPartitions => 1;

    protected int RetriesOf(int index) => CallsTo(_stepIds[index], nameof(IJobStep.Retry));

    protected int Started => _stepIds.Count(id => CallsTo(id, nameof(IJobStep.Start)) > 0);

    protected Task FailByTimeout(int index) => _job.OnStepFailed(
        _stepIds[index],
        JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
            {
                TransientFailure = new(new("partition", Properties.ArrayIndexers.NoIndexers), 42ul, ["Response did not arrive on time"])
            },
            ["Response did not arrive on time"],
            string.Empty)));

    protected Task Succeed(int index) => _job.OnStepSucceeded(
        _stepIds[index],
        JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)));

    protected async Task<bool> ResumeWithObserverNotEnteringReplay()
    {
        _observer.IsSubscribed().Returns(true);
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Active });
        _observer.Replay().Returns(JobId.New());
        return (await _job.Resume()).IsSuccess;
    }

    protected async Task<bool> ResumeWithObserverReplaying()
    {
        _observer.IsSubscribed().Returns(true);
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        return (await _job.Resume()).IsSuccess;
    }

    async Task Establish()
    {
        _silo.AddService(Options.Create(new JobsOptions { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero }));
        var jobsStorage = Substitute.For<IJobsStorage>();
        jobsStorage.GetFor(Arg.Any<string>(), Arg.Any<string>()).Returns(new JobsStorage(_jobStorage, _jobStepStorage));
        _silo.AddService(jobsStorage);
        _job = await _silo.CreateGrainAsync<TestableReplayObserver>(_jobId, _jobKey);
        _observer.GetFailedPartitionKeys().Returns([]);
        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _silo.AddProbe(_ => jobsManager);
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>())
            .Returns(_ => CreateKeys([.. Enumerable.Range(0, NumberOfPartitions).Select(index => new Key($"partition-{index}", Properties.ArrayIndexers.NoIndexers))]));
        _silo.AddProbe<IHandleEventsForPartition>(identity => StepFor(Guid.Parse(identity.ToString().Split('+')[0])));

        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(_ => Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(
                _stepIds.Select(id => new JobStepState
                {
                    Id = new(_jobId, id),
                    Type = typeof(IHandleEventsForPartition),
                    Status = CallsTo(id, nameof(IJobStep.Start)) > 0 ? JobStepStatus.Running : JobStepStatus.Unknown
                }).ToImmutableList())));

        _job.PrepareSynchronouslyForTesting = true;
        var started = await _job.Start(_request with { ObserverType = ObserverType.Reactor, EventTypes = [new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First)] });
        started.IsSuccess.ShouldBeTrue();
    }

    IHandleEventsForPartition StepFor(Guid id)
    {
        if (_stepsById.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var step = Substitute.For<IHandleEventsForPartition>();
        step.Prepare(Arg.Any<object>(), Arg.Any<JobStepStage>()).Returns(Task.FromResult(Result<PrepareJobStepError>.Success()));
        step.Start(Arg.Any<GrainId>()).Returns(Task.FromResult<Result<StartJobStepError>>(StartJobStepError.AlreadyStarted));
        step.Retry(Arg.Any<GrainId>(), Arg.Any<int>()).Returns(Task.FromResult(Result<StartJobStepError>.Success()));
        _stepsById[id] = step;
        _stepIds.Add(id);
        return step;
    }

    int CallsTo(Guid id, string method) => _stepsById[id].ReceivedCalls().Count(call => call.GetMethodInfo().Name == method);
}
