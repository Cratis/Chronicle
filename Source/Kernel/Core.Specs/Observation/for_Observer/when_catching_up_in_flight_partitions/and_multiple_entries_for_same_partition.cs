// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_in_flight_partitions;

public class and_multiple_entries_for_same_partition : given.an_observer_with_subscription_for_specific_event_type
{
    Key _partition = "shared-partition";

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = (EventSequenceNumber)4UL };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(new[]
            {
                new InFlightEvent { ObserverId = _observerId, Partition = _partition, EventSequenceNumber = (EventSequenceNumber)5UL },
                new InFlightEvent { ObserverId = _observerId, Partition = _partition, EventSequenceNumber = (EventSequenceNumber)6UL }
            });
    }

    async Task Because() => await _observer.CatchUpInFlightPartitions();

    [Fact] void should_start_exactly_one_catch_up_job_for_the_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition));
}
