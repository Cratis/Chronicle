// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_controlled_delivery;

public class and_only_a_prefix_has_a_handler : given.a_controlled_observer
{
    readonly List<EventSequenceNumber> _delivered = [];

    void Establish() => _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(call =>
        {
            var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            _delivered.AddRange(events.Select(@event => @event.Context.SequenceNumber));
            return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
        });

    Task Because() => _observer.Handle(partition, [EventAt(event_type, 10), EventAt(added_type, 11), EventAt(event_type, 12)]);

    [Fact] void should_deliver_only_the_capable_prefix() => _delivered.ShouldEqual([new EventSequenceNumber(10)]);
    [Fact] void should_hold_before_any_later_events() => _failedPartitionsState.Partitions.Single().RecoveryCursor!.Position.ShouldEqual((EventSequenceNumber)11UL);
    [Fact] void should_record_the_successful_prefix() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_not_count_a_hold_as_an_attempt() => _failedPartitionsState.Partitions.Single().Attempts.ShouldBeEmpty();
}
