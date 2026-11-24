// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_many_definitions_changed_elsewhere : given.a_registry
{
    ObserverDefinition _after;

    async Task Because()
    {
        await _registry.Get(_observerId);
        for (var i = 0; i <= ObserverDefinitionRegistry.MaxIncrementalRefreshes; i++)
        {
            var id = new ObserverId($"observer-{i}");
            _storage.Seed(DefinitionOf(id, _secondEventType));
            _registry.Invalidate(id);
        }

        _storage.ResetCounts();
        _after = await _registry.Get(new ObserverId("observer-0"));
    }

    [Fact] void should_load_everything_once() => _storage.GetAllCalls.ShouldEqual(1);
    [Fact] void should_not_read_them_one_by_one() => _storage.GetCalls.ShouldEqual(0);
    [Fact] void should_have_the_definitions() => _after.EventTypes.ShouldEqual([_secondEventType]);
}
