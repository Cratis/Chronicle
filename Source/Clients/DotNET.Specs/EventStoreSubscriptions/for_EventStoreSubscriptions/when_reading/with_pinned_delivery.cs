// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.when_reading;

public class with_pinned_delivery : given.subscriptions
{
    void Establish() => _service.GetSubscriptions(Arg.Any<GetEventStoreSubscriptionsRequest>()).Returns([
        new Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition
        {
            Identifier = "s", SourceEventStore = "source",
            EventTypes = [new() { Id = "customer", Generation = 3 }],
            GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned
        }]);
    async Task Because() => _definitions = await _subscriptions.GetAll();
    [Fact] void should_expose_exact_generation_pins() => _definitions.Single().EventTypePins.ShouldContainOnly(new EventType("customer", 3));
    [Fact] void should_expose_the_delivery_policy() => _definitions.Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
