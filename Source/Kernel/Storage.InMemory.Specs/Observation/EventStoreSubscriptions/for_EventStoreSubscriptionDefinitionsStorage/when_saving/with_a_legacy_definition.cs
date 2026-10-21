// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Storage.InMemory.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionsStorage.when_saving;

public class with_a_legacy_definition : Specification
{
    EventStoreSubscriptionDefinition _result;
    async Task Because()
    {
        var storage = new EventStoreSubscriptionDefinitionsStorage();
        await storage.Save(new("subscription", "source", [new EventType("customer", 1)]));
        _result = (await storage.GetAll()).Single();
    }
    [Fact] void should_keep_compatibility_as_the_default() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_preserve_the_original_type() => _result.EventTypes.ShouldContainOnly(new EventType("customer", 1));
}
