// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverSubscription.when_getting_the_subscriber_key;

public class and_the_subscriber_is_partitioned : given.a_subscription
{
    ObserverSubscription _subscription;
    ObserverSubscriberKey[] _keys;

    void Establish() => _subscription = SubscriptionFor(typeof(IProjectionObserverSubscriber));

    void Because() => _keys = [.. partitions.Select(partition => _subscription.GetSubscriberKeyFor(partition, silo_address))];

    [Fact] void should_give_every_partition_its_own_key() => _keys.Distinct().Count().ShouldEqual(partitions.Length);
    [Fact] void should_keep_the_partition_as_the_event_source_id() => _keys.Select(_ => _.EventSourceId.Value).ShouldContainOnly(partitions.Select(_ => _.ToString()));
    [Fact] void should_not_use_the_reserved_all_partitions_value() => _keys.ShouldEachConformTo(_ => _.EventSourceId != ObserverSubscriberKey.AllPartitions);
    [Fact] void should_carry_the_observer_identity() => _keys.ShouldEachConformTo(_ => _.ObserverId == observer_key.ObserverId);
    [Fact] void should_carry_the_silo_address() => _keys.ShouldEachConformTo(_ => _.SiloAddress == silo_address.ToParsableString());
}
