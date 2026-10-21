// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing.with_pinned_delivery;

public class and_the_generation_is_registered : given.an_observer
{
    readonly EventType _pin = new("person-registered", 2);
    void Establish() => _eventTypesStorage.HasFor(_pin.Id, _pin.Generation).Returns(true);
    async Task Because() => await _observer.SubscribeWithGenerationDelivery<NullObserverSubscriber>(ObserverType.Reactor, [_pin], SiloAddress.Zero, EventGenerationDelivery.Pinned);
    [Fact] async Task should_store_the_policy_on_the_subscription() => (await _observer.GetSubscription()).GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_store_the_policy_on_the_definition() => _definitionStorage.State.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_exact_pin() => _definitionStorage.State.EventTypes.ShouldContainOnly(_pin);
}
