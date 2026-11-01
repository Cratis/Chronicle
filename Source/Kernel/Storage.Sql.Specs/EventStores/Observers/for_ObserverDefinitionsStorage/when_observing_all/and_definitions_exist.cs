// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_definitions_exist : given.an_observing_observer_definitions_storage
{
    async Task Because()
    {
        await Seed("first");
        await Subscribe();
    }

    [Fact] void should_emit_one_snapshot() => _emissions.Count.ShouldEqual(1);
    [Fact] void should_map_the_identifier() => _emissions.Single().Single().Identifier.Value.ShouldEqual("first");
    [Fact] void should_map_event_types() => _emissions.Single().Single().EventTypes.Single().Generation.Value.ShouldEqual(2U);
    [Fact] void should_map_delivery() => _emissions.Single().Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
