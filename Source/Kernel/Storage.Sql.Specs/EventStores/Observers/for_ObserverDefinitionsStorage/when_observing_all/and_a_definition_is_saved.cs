// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_definition_is_saved : given.an_observing_observer_definitions_storage
{
    async Task Because()
    {
        await Seed("first");
        await Subscribe();
        var definition = await _storage.Get("first");
        await _storage.Save(definition with { GenerationDelivery = EventGenerationDelivery.Compatibility });
        await WaitForEmission();
    }

    [Fact] void should_emit_the_updated_definition() => _emissions.ToArray()[1].Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
}
