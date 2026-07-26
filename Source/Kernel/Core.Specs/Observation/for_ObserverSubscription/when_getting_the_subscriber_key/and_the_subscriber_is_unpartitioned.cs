// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverSubscription.when_getting_the_subscriber_key;

public class and_the_subscriber_is_unpartitioned : given.a_subscription
{
    ObserverSubscription _subscription;
    ObserverSubscriberKey[] _keys;

    void Establish() => _subscription = SubscriptionFor(typeof(ICollapsingProjectionObserverSubscriber));

    void Because() => _keys = [.. partitions.Select(partition => _subscription.GetSubscriberKeyFor(partition, silo_address))];

    [Fact] void should_give_every_partition_the_same_key() => _keys.Distinct().Count().ShouldEqual(1);
    [Fact] void should_replace_the_partition_with_the_reserved_all_partitions_value() => _keys.ShouldEachConformTo(_ => _.EventSourceId == ObserverSubscriberKey.AllPartitions);
    [Fact] void should_carry_the_observer_identity() => _keys.ShouldEachConformTo(_ => _.ObserverId == observer_key.ObserverId);
    [Fact] void should_round_trip_through_the_grain_key_string() => ObserverSubscriberKey.Parse(_keys[0]).ShouldEqual(_keys[0]);
}
