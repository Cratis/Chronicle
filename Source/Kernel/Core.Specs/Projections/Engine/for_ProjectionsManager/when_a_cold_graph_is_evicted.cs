// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_a_cold_graph_is_evicted : given.a_manager_with_shared_definitions
{
    IProjection _first;
    IProjection _reused;
    IProjection _rebuilt;
    IProjection _cold;
    bool _coldWasCached;

    async Task Because()
    {
        _first = (await _manager.GetCurrent(_eventStore, "tenant-0", _firstDefinition.Identifier))!;
        _cold = (await _manager.GetCurrent(_eventStore, "tenant-1", _firstDefinition.Identifier))!;
        for (var tenant = 2; tenant < ProjectionsManager.CacheCapacity; tenant++)
        {
            await _manager.GetCurrent(_eventStore, $"tenant-{tenant}", _firstDefinition.Identifier);
        }

        _reused = (await _manager.GetCurrent(_eventStore, "tenant-0", _firstDefinition.Identifier))!;
        await _manager.GetCurrent(_eventStore, "overflow", _firstDefinition.Identifier);
        _coldWasCached = _manager.TryGet(_eventStore, "tenant-1", _firstDefinition.Identifier, out _);
        _rebuilt = (await _manager.GetCurrent(_eventStore, "tenant-1", _firstDefinition.Identifier))!;
    }

    [Fact] void should_reuse_a_hot_graph() => _reused.ShouldEqual(_first);
    [Fact] void should_evict_the_least_recently_used_graph() => _coldWasCached.ShouldBeFalse();
    [Fact] void should_rebuild_a_cold_graph_on_demand() => _rebuilt.ShouldNotEqual(_cold);
    [Fact] void should_stay_bounded_after_rebuilding() => _manager.CachedGraphCount.ShouldEqual(ProjectionsManager.CacheCapacity);
    [Fact] void should_retain_definition_metadata() => _manager.TryGetDefinitionFingerprint(_eventStore, _firstDefinition.Identifier, out _).ShouldBeTrue();
}
