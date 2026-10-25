// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Dropping a job removes it, which can take a while. The observer drops a bounded batch per turn and leaves the
/// rest for later turns, so the jobs of a long-neglected observer do not keep it busy past a grain call's timeout.
/// </summary>
public class and_more_retry_jobs_can_never_run_than_one_turn_drops : given.an_observer
{
    const int JobCount = (Observer.MaxRetryJobDropsPerTurn * 2) + 1;

    readonly List<JobState> _jobs = [];
    readonly HashSet<JobId> _dropped = [];
    int _droppedAfterFirstTurn;
    int _droppedAfterSecondTurn;
    int _droppedAfterAllTurns;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition("event-source-failed");
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };

        for (var job = 0; job < JobCount; job++)
        {
            _jobs.Add(given.stopped_retry_jobs.NeverPrepared(_observerKey, $"event-source-{job}"));
        }

        // A dropped job is gone from the next listing.
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(_jobs.ToImmutableList()));
        _jobsManager.Delete(Arg.Any<JobId>()).Returns(call =>
        {
            var id = call.Arg<JobId>();
            _dropped.Add(id);
            _jobs.RemoveAll(_ => _.Id == id);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _droppedAfterFirstTurn = _dropped.Count;
        await _silo.TimerRegistry.FireAllAsync();
        _droppedAfterSecondTurn = _dropped.Count;
        await _silo.TimerRegistry.FireAllAsync();
        _droppedAfterAllTurns = _dropped.Count;
    }

    [Fact] void should_drop_one_batch_in_the_first_turn() => _droppedAfterFirstTurn.ShouldEqual(Observer.MaxRetryJobDropsPerTurn);
    [Fact] void should_drop_another_batch_in_the_next_turn() => _droppedAfterSecondTurn.ShouldEqual(Observer.MaxRetryJobDropsPerTurn * 2);
    [Fact] void should_eventually_drop_every_job() => _droppedAfterAllTurns.ShouldEqual(JobCount);
}
