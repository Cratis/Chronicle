// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing.with_pinned_delivery;

public class and_projection_clauses_name_different_generations : given.an_observer
{
    readonly EventType _first = new("person-registered", 1);
    readonly EventType _second = new("person-registered", 2);

    Exception _error;

    void Establish() => _eventTypesStorage.HasFor(_first.Id, Arg.Any<EventTypeGeneration>()).Returns(true);

    async Task Because() => _error = await Catch.Exception(() => _observer.SubscribeWithGenerationDelivery<NullObserverSubscriber>(ObserverType.Projection, [_first, _second], SiloAddress.Zero, EventGenerationDelivery.Pinned));

    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
}
