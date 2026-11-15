// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

public class and_writes_fail_for_one_partition_and_one_outside_the_page : given.a_page_subscriber
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _writeFailures.Add(new Storage.Sinks.FailedPartition((Key)"second", 2UL) { Reason = "write failed" });
        _writeFailures.Add(new Storage.Sinks.FailedPartition((Key)"outside", 7UL) { Reason = "dropped with the batch" });
    }

    Task Because() => Deliver();

    [Fact] void should_fail_the_partition_whose_write_failed() => ResultFor("second").State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_nothing_of_it_as_handled() => ResultFor("second").LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_settle_the_other_partition_as_handled() => ResultFor("first").State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_record_the_partition_outside_the_page_as_failed() => _observer.Received(1).PartitionFailed((Key)"outside", (EventSequenceNumber)7UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_not_record_the_partition_in_the_page_through_the_observer() => _observer.DidNotReceive().PartitionFailed((Key)"second", Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}
