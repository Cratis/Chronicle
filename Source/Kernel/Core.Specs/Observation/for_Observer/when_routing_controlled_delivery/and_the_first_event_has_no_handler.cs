// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_controlled_delivery;

public class and_the_first_event_has_no_handler : given.a_controlled_observer
{
    Task Because() => _observer.Handle(partition, [EventAt(added_type, 10), EventAt(event_type, 11)]);

    [Fact] void should_not_deliver_any_events() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_hold_the_partition() => _failedPartitionsState.Partitions.Single().Hold!.AwaitedEventType.ShouldEqual(added_type.Id);
    [Fact] void should_resume_at_the_unhandled_event() => _failedPartitionsState.Partitions.Single().RecoveryCursor.ShouldEqual(new FailedPartitionRecoveryCursor(10UL, FailedPartitionRecoveryMode.Live, null));
    [Fact] void should_not_record_a_failed_attempt() => _failedPartitionsState.Partitions.Single().Attempts.ShouldBeEmpty();
    [Fact] void should_not_quarantine() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
}
