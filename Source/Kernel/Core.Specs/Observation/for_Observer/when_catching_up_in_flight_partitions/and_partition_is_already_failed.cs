// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_in_flight_partitions;

public class and_partition_is_already_failed : given.an_observer_with_subscription_for_specific_event_type
{
    Key _partition = "failing-partition";
    FailedPartitions _failedPartitions;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.AddFailedPartition(_partition);
        _failedPartitionsStorage.State = _failedPartitions;
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = (EventSequenceNumber)4UL };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(new[]
            {
                new InFlightEvent { ObserverId = _observerId, Partition = _partition, EventSequenceNumber = (EventSequenceNumber)5UL }
            });
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.CatchUpInFlightPartitions();

    [Fact] void should_not_start_any_catch_up_job_for_failing_partition() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition));
}
