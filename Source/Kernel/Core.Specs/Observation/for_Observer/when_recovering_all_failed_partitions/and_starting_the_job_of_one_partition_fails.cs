// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// A job that cannot be started - its grain could not activate while the silo was busy - must not end the turn: the
/// partitions queued behind it would wait for a subscription that may never come.
/// </summary>
public class and_starting_the_job_of_one_partition_fails : given.an_observer
{
    const string FailingPartition = "event-source-0";

    static int _failingPartitionStarts;
    static int _startedOtherPartitions;
    static int _startsAfterTheNextTurn;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        for (var partition = 0; partition < 3; partition++)
        {
            failedPartitions.AddFailedPartition($"event-source-{partition}");
        }

        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 3 };

        // The first start of the partition fails, as an activation that times out does. Later starts succeed.
        var failed = false;
        _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(call =>
            {
                if (call.Arg<RetryFailedPartitionRequest>().Key.ToString() == FailingPartition && !failed)
                {
                    failed = true;
                    return Task.FromException<Result<JobId, StartJobError>>(new TimeoutException("The grain did not activate"));
                }

                return Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New()));
            });
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _failingPartitionStarts = StartsOf(FailingPartition);
        _startedOtherPartitions = StartedPartitions().Distinct().Count(_ => _ != FailingPartition);

        await _silo.TimerRegistry.FireAllAsync();
        _startsAfterTheNextTurn = StartsOf(FailingPartition);
    }

    [Fact] void should_try_to_start_the_partition_that_failed() => _failingPartitionStarts.ShouldEqual(1);
    [Fact] void should_still_recover_the_partitions_queued_behind_it() => _startedOtherPartitions.ShouldEqual(2);
    [Fact] void should_try_the_failed_partition_again_in_a_later_turn() => _startsAfterTheNextTurn.ShouldEqual(2);

    int StartsOf(string partition) => StartedPartitions().Count(_ => _ == partition);

    IEnumerable<string> StartedPartitions() => _jobsManager.ReceivedCalls()
        .Where(call => call.GetArguments().FirstOrDefault() is RetryFailedPartitionRequest)
        .Select(call => ((RetryFailedPartitionRequest)call.GetArguments()[0]!).Key.ToString()!);
}
