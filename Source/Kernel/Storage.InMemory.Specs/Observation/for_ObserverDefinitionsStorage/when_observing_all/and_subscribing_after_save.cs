// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_subscribing_after_save : given.an_observed_storage
{
    ObserverDefinition[] _initial;

    async Task Because()
    {
        await _storage.Save(Definition());
        using var subscription = _storage.ObserveAll().Subscribe(definitions => _initial = definitions.ToArray());
    }

    [Fact] void should_replay_the_current_set() => _initial.Single().Identifier.Value.ShouldEqual("observer");
    [Fact] void should_not_mutate_the_previous_snapshot() => _emissions[0].ShouldBeEmpty();
}
