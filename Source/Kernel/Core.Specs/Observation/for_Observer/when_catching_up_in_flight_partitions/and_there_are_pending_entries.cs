// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_in_flight_partitions;

public class and_there_are_pending_entries : given.an_observer_with_subscription_for_specific_event_type
{
    Key _firstPartition = "partition-one";
    Key _secondPartition = "partition-two";

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = (EventSequenceNumber)9UL };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(new[]
            {
                new InFlightEvent { ObserverId = _observerId, Partition = _firstPartition, EventSequenceNumber = (EventSequenceNumber)10UL },
                new InFlightEvent { ObserverId = _observerId, Partition = _secondPartition, EventSequenceNumber = (EventSequenceNumber)11UL }
            });
    }

    async Task Because() => await _observer.CatchUpInFlightPartitions();

    [Fact] void should_start_catch_up_for_first_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ =>
                _.Key == _firstPartition &&
                _.FromSequenceNumber == (EventSequenceNumber)10UL));

    [Fact] void should_start_catch_up_for_second_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ =>
                _.Key == _secondPartition &&
                _.FromSequenceNumber == (EventSequenceNumber)10UL));

    [Fact] void should_mark_first_partition_as_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_firstPartition);
    [Fact] void should_mark_second_partition_as_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_secondPartition);
}
