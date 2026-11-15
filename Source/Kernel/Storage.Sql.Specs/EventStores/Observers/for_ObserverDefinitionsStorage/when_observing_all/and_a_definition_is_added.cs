// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_definition_is_added : given.an_observing_observer_definitions_storage
{
    async Task Because()
    {
        await Subscribe();
        await Seed("added");
        await WaitForEmission();
    }

    [Fact] void should_emit_the_added_definition() => _emissions.ToArray()[1].Single().Identifier.Value.ShouldEqual("added");
}
