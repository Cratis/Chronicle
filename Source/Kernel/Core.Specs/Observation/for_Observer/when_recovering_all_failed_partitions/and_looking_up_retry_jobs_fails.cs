// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Looking up the jobs is the first thing a recovery turn does. When it fails once, the turn must not fail with it:
/// the failed partition is still recovered.
/// </summary>
public class and_looking_up_retry_jobs_fails : given.an_observer
{
    const string Partition = "event-source-a";

    Exception? _error;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition(Partition);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        // Only the lookups made by the recovery turn fail, and only the first of them.
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(
            Task.FromException<IImmutableList<JobState>>(new InvalidOperationException("Lookup failed")),
            Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _error = await Catch.Exception(_silo.TimerRegistry.FireAllAsync);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();

    [Fact]
    void should_still_start_recovering_the_partition() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
