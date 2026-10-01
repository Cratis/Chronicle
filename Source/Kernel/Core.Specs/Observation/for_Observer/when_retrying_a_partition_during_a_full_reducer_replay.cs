// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_retrying_a_partition_during_a_full_reducer_replay : given.an_observer_with_subscription
{
    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Reducer };
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        _failedPartitionsState.Partitions = [new() { Partition = (Key)"partition" }];
    }
    async Task Because() => await _observer.TryStartRecoverJobForFailedPartition((Key)"partition");
    [Fact] void should_not_retry_an_isolated_replay_failure_against_the_published_model() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
