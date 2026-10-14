// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Storage.InMemory.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionsStorage.when_saving;

public class with_pinned_generations : Specification
{
    EventStoreSubscriptionDefinition _definition;
    EventStoreSubscriptionDefinition _result;
    void Establish() => _definition = new("subscription", "source", [new EventType("customer", 3), new EventType("order", 2)]) { GenerationDelivery = EventGenerationDelivery.Pinned };
    async Task Because()
    {
        var storage = new EventStoreSubscriptionDefinitionsStorage();
        await storage.Save(_definition);
        _result = (await storage.Get("subscription"))!;
    }
    [Fact] void should_preserve_the_mode() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_exact_generation_pins() => _result.EventTypes.ShouldContainOnly(_definition.EventTypes);
}
