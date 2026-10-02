// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_client_disconnected_during_replay_with_failed_partitions : given.a_replaying_observer_with_client_subscription
{
    FailedPartition _failedPartition;
    List<RetryFailedPartitionRequest> _retryRequests;
    int _retryCountDuringSubscription;

    async Task Establish()
    {
        var failures = new FailedPartitions();
        _failedPartition = failures.AddFailedPartition("failed-partition");
        _failedPartitionsStorage.State = failures;
        await _failedPartitionsStorage.WriteStateAsync();
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        await _stateStorage.WriteStateAsync();
        _retryRequests = [];
        _jobsManager.When(_ => _.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>()))
            .Do(call => _retryRequests.Add(call.Arg<RetryFailedPartitionRequest>()));
        await DisconnectClient();
    }

    async Task Because()
    {
        await _observer.Subscribe<IClientOwnedObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _connectedClient);
        _retryCountDuringSubscription = _retryRequests.Count;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_recover_partitions_within_the_subscription_call() => _retryCountDuringSubscription.ShouldEqual(0);
    [Fact] void should_recover_the_failed_partition_in_a_separate_turn() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _failedPartition.Partition && _.FromSequenceNumber == _failedPartition.LastAttempt.SequenceNumber && _.EventTypes.SequenceEqual(new[] { event_type })));
    [Fact] async Task should_return_to_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_keep_the_unresolved_failure() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
