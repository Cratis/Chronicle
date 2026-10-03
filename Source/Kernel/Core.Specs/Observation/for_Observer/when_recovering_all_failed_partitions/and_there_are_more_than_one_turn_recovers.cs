// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Recovering a partition starts a job for it, which is slow enough that recovering hundreds in one turn kept the
/// observer busy past a grain call's response timeout - so the subscribe of a reconnecting client never reached it.
/// The observer recovers a bounded batch per turn and leaves the rest for later turns.
/// </summary>
public class and_there_are_more_than_one_turn_recovers : given.an_observer
{
    const int PartitionCount = (Observer.MaxPartitionRecoveriesPerTurn * 2) + 3;

    static int _startedAfterFirstTurn;
    static int _startedAfterSecondTurn;
    static int _startedAfterAllTurns;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        for (var partition = 0; partition < PartitionCount; partition++)
        {
            failedPartitions.AddFailedPartition($"event-source-{partition}");
        }

        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = PartitionCount };
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _startedAfterFirstTurn = RecoveryJobsStarted();
        await _silo.TimerRegistry.FireAllAsync();
        _startedAfterSecondTurn = RecoveryJobsStarted();
        await _silo.TimerRegistry.FireAllAsync();
        _startedAfterAllTurns = RecoveryJobsStarted();
    }

    [Fact] void should_recover_one_batch_in_the_first_turn() => _startedAfterFirstTurn.ShouldEqual(Observer.MaxPartitionRecoveriesPerTurn);
    [Fact] void should_recover_another_batch_in_the_next_turn() => _startedAfterSecondTurn.ShouldEqual(Observer.MaxPartitionRecoveriesPerTurn * 2);
    [Fact] void should_eventually_recover_every_partition() => _startedAfterAllTurns.ShouldEqual(PartitionCount);
    [Fact]
    void should_recover_each_partition_only_once() => _jobsManager.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(IJobsManager.Start))
        .Select(call => ((RetryFailedPartitionRequest)call.GetArguments()[0]!).Key.ToString())
        .Distinct()
        .Count()
        .ShouldEqual(PartitionCount);

    int RecoveryJobsStarted() => _jobsManager.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(IJobsManager.Start) && call.GetArguments()[0] is RetryFailedPartitionRequest);
}
