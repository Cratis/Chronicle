// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_batch_exceeds_the_live_delivery_budget : given.an_observer_with_a_live_delivery_batch
{
    async Task Because() => await _observer.Handle(Partition, _events);

    [Fact] void should_not_call_the_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_start_a_catch_up_job_from_the_first_event() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(request => request.Key == Partition && request.FromSequenceNumber == FirstEvent));
    [Fact] void should_mark_the_partition_as_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(Partition);
    [Fact] void should_not_advance_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_not_mark_the_partition_as_in_flight() => _stateStorage.State.InFlightPartitions.ShouldNotContain(Partition);
}
