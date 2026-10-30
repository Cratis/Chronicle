// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// The sweep and the recovery of a partition share one budget of job drops per turn, so a partition whose retained
/// jobs number in the hundreds cannot keep the observer busy past a grain call's timeout.
/// </summary>
public class and_every_retry_job_that_can_never_run_matches_one_failed_partition : given.an_observer
{
    const string Partition = "event-source-a";
    const int JobCount = (Observer.MaxRetryJobDropsPerTurn * 2) + 5;

    int _deletes;
    int _deletesAfterFirstTurn;
    int _deletesAfterSecondTurn;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition(Partition);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };

        HasOutstandingJobs([.. Enumerable.Range(0, JobCount).Select(_ => given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition))]);
        _jobsManager.When(_ => _.Delete(Arg.Any<JobId>())).Do(_ => _deletes++);
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _deletesAfterFirstTurn = _deletes;
        await _silo.TimerRegistry.FireAllAsync();
        _deletesAfterSecondTurn = _deletes;
    }

    [Fact] void should_not_drop_more_than_one_budget_in_the_first_turn() => _deletesAfterFirstTurn.ShouldEqual(Observer.MaxRetryJobDropsPerTurn);
    [Fact] void should_not_drop_more_than_one_budget_in_the_second_turn() => (_deletesAfterSecondTurn - _deletesAfterFirstTurn).ShouldEqual(Observer.MaxRetryJobDropsPerTurn);
}
