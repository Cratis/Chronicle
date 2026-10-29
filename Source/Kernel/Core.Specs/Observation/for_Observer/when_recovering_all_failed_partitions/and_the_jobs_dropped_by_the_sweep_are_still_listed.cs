// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Deleting a job returns normally even when it could not be removed. When the jobs the sweep dropped are listed again
/// in the next turn, dropping makes no progress, and an immediate turn after turn would only hot-loop.
/// </summary>
public class and_the_jobs_dropped_by_the_sweep_are_still_listed : given.an_observer
{
    const string Partition = "event-source-a";
    const int JobCount = (Observer.MaxRetryJobDropsPerTurn * 2) + 5;

    int _deletes;
    int _deletesAfterSecondTurn;
    int _deletesAfterAllTurns;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition(Partition);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };

        HasOutstandingJobs([.. Enumerable.Range(0, JobCount).Select(_ => given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition))]);

        // Deleting is reported as done, but the jobs stay listed.
        _jobsManager.Delete(Arg.Any<JobId>()).Returns(_ =>
        {
            _deletes++;
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
        _deletesAfterSecondTurn = _deletes;
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
        _deletesAfterAllTurns = _deletes;
    }

    [Fact] void should_not_schedule_another_turn_once_it_makes_no_progress() => _deletesAfterAllTurns.ShouldEqual(_deletesAfterSecondTurn);
}
