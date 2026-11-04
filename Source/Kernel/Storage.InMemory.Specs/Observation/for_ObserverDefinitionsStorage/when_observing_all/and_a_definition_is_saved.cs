// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_definition_is_saved : given.an_observed_storage
{
    Task Because() => _storage.Save(Definition());

    [Fact] void should_emit_a_second_snapshot() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_include_the_definition() => _emissions[^1].Single().Identifier.Value.ShouldEqual("observer");
}
