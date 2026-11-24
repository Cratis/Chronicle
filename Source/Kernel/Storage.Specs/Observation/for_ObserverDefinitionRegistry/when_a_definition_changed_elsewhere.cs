// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_a_definition_changed_elsewhere : given.a_registry
{
    ObserverDefinition _before;
    ObserverDefinition _after;

    async Task Because()
    {
        _before = await _registry.Get(_observerId);
        _storage.Seed(DefinitionOf(_observerId, _secondEventType));
        _storage.ResetCounts();
        _registry.Invalidate(_observerId);
        _after = await _registry.Get(_observerId);
    }

    [Fact] void should_give_the_changed_definition() => _after.EventTypes.ShouldEqual([_secondEventType]);
    [Fact] void should_read_only_that_definition() => _storage.GetCalls.ShouldEqual(1);
    [Fact] void should_not_load_everything_again() => _storage.GetAllCalls.ShouldEqual(0);
}
