// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_a_definition_was_registered_elsewhere_and_not_announced : given.a_registry
{
    static readonly ObserverId _newObserverId = "a-new-observer";

    ObserverDefinition _first;
    ObserverDefinition _second;

    async Task Because()
    {
        await _registry.Get(_observerId);
        _storage.Seed(DefinitionOf(_newObserverId, _secondEventType));
        _first = await _registry.Get(_newObserverId);
        _storage.ResetCounts();
        _second = await _registry.Get(_newObserverId);
    }

    [Fact] void should_find_it() => _first.EventTypes.ShouldEqual([_secondEventType]);
    [Fact] void should_serve_it_from_the_registry_afterwards() => _storage.Reads.ShouldEqual(0);
    [Fact] void should_give_the_same_instance_afterwards() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
