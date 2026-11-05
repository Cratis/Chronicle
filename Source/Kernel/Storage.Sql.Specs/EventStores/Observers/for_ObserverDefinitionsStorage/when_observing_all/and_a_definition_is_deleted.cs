// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_definition_is_deleted : given.an_observing_observer_definitions_storage
{
    async Task Because()
    {
        await Seed("first");
        await Subscribe();
        await _storage.Delete("first");
        await WaitForEmission();
    }

    [Fact] void should_emit_the_empty_set() => _emissions.ToArray()[1].ShouldBeEmpty();
}
