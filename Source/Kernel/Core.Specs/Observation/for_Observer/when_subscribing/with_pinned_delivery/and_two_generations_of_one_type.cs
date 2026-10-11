// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing.with_pinned_delivery;

public class and_two_generations_of_one_type : given.an_observer
{
    Exception _error;
    async Task Because() => _error = await Catch.Exception(() => _observer.SubscribeWithGenerationDelivery<NullObserverSubscriber>(ObserverType.Reactor, [new EventType("person-registered", 1), new EventType("person-registered", 2)], SiloAddress.Zero, EventGenerationDelivery.Pinned));
    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
