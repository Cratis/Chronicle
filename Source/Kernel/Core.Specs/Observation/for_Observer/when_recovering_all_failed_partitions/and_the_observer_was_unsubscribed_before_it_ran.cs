// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// A recovery job prepares against the observer's subscriber, so with the client gone every job fails to prepare and
/// the partitions stay failed. Starting them only keeps the observer busy while the client is trying to reconnect;
/// the client's next subscribe asks for the recovery again.
/// </summary>
public class and_the_observer_was_unsubscribed_before_it_ran : given.an_observer
{
    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition("some-event-source");
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.Unsubscribe();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact]
    void should_not_start_recovering_the_partition() => _jobsManager
        .DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
