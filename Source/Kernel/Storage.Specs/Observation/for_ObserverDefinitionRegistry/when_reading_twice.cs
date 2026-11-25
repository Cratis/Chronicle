// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_reading_twice : given.a_registry
{
    ObserverDefinition _first;
    ObserverDefinition _second;

    async Task Because()
    {
        _first = await _registry.Get(_observerId);
        _second = await _registry.Get(_observerId);
    }

    [Fact] void should_load_all_definitions_once() => _storage.GetAllCalls.ShouldEqual(1);
    [Fact] void should_not_read_a_single_definition() => _storage.GetCalls.ShouldEqual(0);
    [Fact] void should_give_the_same_instance_both_times() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_give_the_stored_content() => _first.EventTypes.ShouldEqual([_firstEventType]);
}
