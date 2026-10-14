// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_reading;

public class with_a_pinned_definition : given.an_event_store_subscriptions_service
{
    IEnumerable<Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> _result;
    void Establish() => _storage.GetEventStore("target").EventStoreSubscriptions.GetAll().Returns([
        new Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition("subscription", "source", [new EventType("customer", 3)]) { GenerationDelivery = EventGenerationDelivery.Pinned }]);
    async Task Because() => _result = await _service.GetSubscriptions(new GetEventStoreSubscriptionsRequest { TargetEventStore = "target" });
    [Fact] void should_expose_the_persisted_generation() => _result.Single().EventTypes.Single().Generation.ShouldEqual(3u);
    [Fact] void should_expose_the_persisted_delivery_policy() => _result.Single().GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
