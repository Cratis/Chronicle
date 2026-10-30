// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_starting_the_catch_up_job_fails : given.an_observer_with_a_live_delivery_batch
{
    const string FailureMessage = "Could not start catch-up";

    void Establish() => _jobsManager
        .When(manager => manager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
        .Throws(new ExceptionWithPreDefinedStackTrace(FailureMessage, "Catch-up startup"));

    async Task Because() => await _observer.Handle(Partition, _events);

    [Fact] void should_fail_the_partition() => _failedPartitionsState.IsFailed(Partition).ShouldBeTrue();
    [Fact] void should_retry_from_the_first_event() => _failedPartitionsState.Partitions.Single().Attempts.First().SequenceNumber.ShouldEqual(FirstEvent);
    [Fact] void should_record_the_failure() => _failedPartitionsState.Partitions.Single().Attempts.First().Messages.ShouldContain(FailureMessage);
    [Fact] void should_not_leave_the_partition_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(Partition);
    [Fact] void should_not_advance_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_not_call_the_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
