// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_controlled_delivery;

public class and_an_uncovered_event_is_below_its_join_fence : given.a_controlled_observer
{
    void Establish()
    {
        var members = subscription.TypeSetMembers!.ToDictionary(member => member.Key, member => member.Value);
        members[added_type.Id] = new ObserverTypeSetMember(new(1), 20UL);
        subscription = subscription with { TypeSetMembers = members };
        _observer.SetSubscription(subscription);
    }

    Task Because() => _observer.Handle(partition, [EventAt(added_type, 10), EventAt(event_type, 11)]);

    [Fact] void should_not_hold_for_ineligible_events() => _failedPartitionsState.Partitions.ShouldBeEmpty();
    [Fact] void should_deliver_only_live_eligible_events() => _subscriber.Received(1).OnNext(partition, Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == 1 && events.Single().Context.SequenceNumber == (EventSequenceNumber)11UL), Arg.Any<ObserverSubscriberContext>());
}
