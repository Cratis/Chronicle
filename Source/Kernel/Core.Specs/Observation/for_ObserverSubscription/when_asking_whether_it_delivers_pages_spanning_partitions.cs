// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Observation.for_ObserverSubscription;

public class when_asking_whether_it_delivers_pages_spanning_partitions : Specification
{
    static readonly ObserverKey _observerKey = new("the-observer", "some-event-store", "some-namespace", EventSequenceId.Log);

    [Fact] void should_deliver_pages_to_a_reducer_that_declared_them() =>
        SubscriptionFor(typeof(IReducerPageObserverSubscriber)).DeliversPartitionPages.ShouldBeTrue();

    [Fact] void should_not_deliver_pages_to_a_reducer_that_did_not_declare_them() =>
        SubscriptionFor(typeof(IReducerObserverSubscriber)).DeliversPartitionPages.ShouldBeFalse();

    [Fact] void should_deliver_pages_to_a_reactor_that_declared_them() =>
        SubscriptionFor(typeof(IReactorPageObserverSubscriber)).DeliversPartitionPages.ShouldBeTrue();

    static ObserverSubscription SubscriptionFor(Type subscriberType) => new(
        _observerKey.ObserverId,
        _observerKey,
        [],
        subscriberType,
        SiloAddress.FromParsableString("127.0.0.1:11111@1"));
}
