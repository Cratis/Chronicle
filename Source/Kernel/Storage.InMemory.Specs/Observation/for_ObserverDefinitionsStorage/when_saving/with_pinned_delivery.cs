// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_saving;

public class with_pinned_delivery : Specification
{
    Chronicle.Storage.Observation.ObserverDefinition _result;
    async Task Because()
    {
        var storage = new ObserverDefinitionsStorage();
        await storage.Save(new("observer", [new EventType("person-registered", 2)], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true) { GenerationDelivery = EventGenerationDelivery.Pinned });
        _result = await storage.Get("observer");
    }
    [Fact] void should_preserve_the_policy() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_pin() => _result.EventTypes.Single().Generation.Value.ShouldEqual(2U);
}
