// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_definition_is_deleted : given.an_observed_storage
{
    async Task Because()
    {
        await _storage.Save(Definition());
        await _storage.Delete("observer");
    }

    [Fact] void should_emit_after_deleting() => _emissions.Count.ShouldEqual(3);
    [Fact] void should_remove_the_definition() => _emissions[^1].ShouldBeEmpty();
}
