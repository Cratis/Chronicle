// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.given;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public abstract class an_observer_catching_up_during_appends : an_observer_with_a_handover_log
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    static readonly Key[] _keys = ["P", "Q"];
    readonly TaskCompletionSource _openQ = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobId _startedJob;

    protected abstract bool AppendBeforePReads { get; }
    protected EventSequenceNumber AcceptedNext { get; private set; }
    protected IReadOnlyList<EventSequenceNumber> FirstLapDeliveries { get; private set; }
    protected EventSequenceNumber? SuccessorStart { get; private set; }

    protected async Task RunCatchUpDuringAppends()
    {
        await _observer.CatchUp();
        var before = new ReaderRound(_startedJob);
        var steps = await before.Prepare(_wide!, _observer, _keys);
        var q = ReadQ(steps.Single(_ => Partition(_) == (Key)"Q"));
        if (AppendBeforePReads) AppendSuffix();
        await RecordRead(before, steps.Single(_ => Partition(_) == (Key)"P"));
        if (!AppendBeforePReads) AppendSuffix();
        _openQ.SetResult();
        await before.Complete(steps.Count, await q);
        FirstLapDeliveries = [.. _delivered];
        AcceptedNext = PersistedObserverState.NextEventSequenceNumber;

        await Reactivate();
        _observer.SetSubscription(subscription);
        _wide = null;
        await _observer.CatchUp();
        if (_wide is not { } suffix) return;
        SuccessorStart = suffix.FromEventSequenceNumber;
        var after = new ReaderRound(_startedJob);
        foreach (var step in await after.Prepare(suffix, _observer, _keys)) await Read(step);
    }


    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Reactor, EventTypes = [event_type] };
        Append("Q", 10UL);
        Append("P", 11UL);
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(call =>
            {
                _wide = call.Arg<CatchUpObserverRequest>();
                _startedJob = JobId.New();
                _jobStorage.GetJob(_startedJob).Returns(Task.FromResult<Catch<JobState, Cratis.Choreography.Storage.Jobs.JobError>>(new JobState
                {
                    Id = _startedJob,
                    Status = JobStatus.Running,
                    Request = _wide
                }));
                return Result<JobId, StartJobError>.Success(_startedJob);
            });
    }

    static Key Partition(JobStepDetails step) => ((HandleEventsForPartitionArguments)step.Request).Partition;

    void AppendSuffix()
    {
        Append("P", 12UL);
        Append("Q", 13UL);
    }

    async Task RecordRead(ReaderRound round, JobStepDetails step) => await round.Record(step.Id, await Read(step));

    async Task<(JobStepId Id, EventSequenceNumber Last)> ReadQ(JobStepDetails step)
    {
        await _openQ.Task.WaitAsync(_deadline, TimeProvider.System);
        return (step.Id, await Read(step));
    }

    Task<EventSequenceNumber> Read(JobStepDetails step)
    {
        var request = (HandleEventsForPartitionArguments)step.Request;
        return DeliverRange(request.StartEventSequenceNumber, request.EndEventSequenceNumber, request.EventTypes, request.Partition);
    }

    sealed class ReaderRound(JobId jobId) : a_catch_up_observer_job
    {
        internal async Task<IImmutableList<JobStepDetails>> Prepare(CatchUpObserverRequest request, IObserver owner, Key[] keys)
        {
            await SetUpJob();
            _request = request;
            _keyIndex.GetKeys(request.FromEventSequenceNumber).Returns(CreateKeys(keys));
            _observer.GetSubscription().Returns(_ => owner.GetSubscription());
            _observer.RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>()).Returns(call => owner.RegisterCatchingUpPartitions(call.Arg<IEnumerable<Key>>()));
            _observer.CaughtUp(jobId, Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>())
                .Returns(call => owner.CaughtUp(jobId, call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSequenceNumber>(2)));
            await StartAndLetTheStepsCome();
            return _job.PreparedSteps!;
        }

        internal Task Record(JobStepId id, EventSequenceNumber last) => _job.RecordStepForTesting(id,
            JobStepResult.Succeeded(new HandleEventsForPartitionResult(last) { LastScannedEventSequenceNumber = last }));

        internal async Task Complete(int count, (JobStepId Id, EventSequenceNumber Last) final)
        {
            await Record(final.Id, final.Last);
            _stateStorage.State.Progress.TotalSteps = count;
            _stateStorage.State.Progress.SuccessfulSteps = count;
            await _job.CompleteForTesting();
        }

        protected override Task CreateJob()
        {
            _jobId = jobId;
            return base.CreateJob();
        }
    }
}
