// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_nothing_was_saved : given.an_observed_storage
{
    [Fact] void should_emit_one_snapshot() => _emissions.Count.ShouldEqual(1);
    [Fact] void should_emit_no_definitions() => _emissions.Single().ShouldBeEmpty();
}
