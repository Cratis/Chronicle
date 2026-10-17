// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_recovering_a_failed_partition_of_an_observer_with_event_types : given.an_observer_with_subscription
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    const string Partition = "SomePartition";

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { EventTypes = [_eventType] };
        _failedPartitionsState.Partitions = [new() { Partition = (Key)Partition }];
    }

    async Task Because() => await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_start_the_recover_job_with_an_explicit_scope() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ => !_.ReaderScope!.IsWildcard && _.ReaderScope.EventTypes.SequenceEqual(new[] { _eventType })));
}
