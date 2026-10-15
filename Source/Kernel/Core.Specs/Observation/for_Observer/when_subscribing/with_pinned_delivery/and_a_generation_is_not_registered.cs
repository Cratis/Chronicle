// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing.with_pinned_delivery;

public class and_a_generation_is_not_registered : given.an_observer
{
    Exception _error;
    ObserverSubscription _before;
    async Task Establish() => _before = await _observer.GetSubscription();
    async Task Because() => _error = await Catch.Exception(() => _observer.SubscribeWithGenerationDelivery<NullObserverSubscriber>(ObserverType.Reactor, [new EventType("person-registered", 2)], SiloAddress.Zero, EventGenerationDelivery.Pinned));
    [Fact] void should_reject_the_generation() => _error.ShouldBeOfExactType<EventTypeGenerationNotRegisteredForObserver>();
    [Fact] async Task should_leave_the_subscription_unchanged() => (await _observer.GetSubscription()).ShouldEqual(_before);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
