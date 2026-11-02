// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache;

[Collection(UnloadingCollection.Name)]
public class when_the_registry_is_collected : Specification
{
    readonly EventTypeRegistryCache<Type[]> _cache = new();
    WeakReference _droppedEventType;
    WeakReference _droppedRegistry;
    WeakReference _liveEventType;
    CollectibleDispatchTypes.Registry _liveRegistry;
    Type[] _liveResult;
    bool _collected;

    void Because()
    {
        (_droppedEventType, _droppedRegistry) = CacheForDroppedRegistry(_cache);
        (_liveEventType, _liveRegistry) = CacheForLiveRegistry(_cache);
        _collected = CollectibleDispatchTypes.Collect(_droppedEventType, _droppedRegistry);
        _liveResult = _cache.Get(_liveRegistry, typeof(string), null, types => [.. types]);
    }

    [Fact] void should_release_the_registry() => _droppedRegistry.IsAlive.ShouldBeFalse();
    [Fact] void should_release_the_cached_collectible_types() => _droppedEventType.IsAlive.ShouldBeFalse();
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();
    [Fact] void should_keep_the_types_of_a_live_registry() => _liveEventType.IsAlive.ShouldBeTrue();
    [Fact] void should_keep_the_result_of_a_live_registry_usable() => _liveResult.ShouldContainOnly((Type)_liveEventType.Target!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference EventType, WeakReference Registry) CacheForDroppedRegistry(EventTypeRegistryCache<Type[]> cache)
    {
        var eventType = CollectibleDispatchTypes.DefineEvent();
        var registry = new CollectibleDispatchTypes.Registry([eventType]);
        cache.Get(registry, typeof(string), null, types => [.. types]);
        return (new WeakReference(eventType), new WeakReference(registry));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference EventType, CollectibleDispatchTypes.Registry Registry) CacheForLiveRegistry(EventTypeRegistryCache<Type[]> cache)
    {
        var eventType = CollectibleDispatchTypes.DefineEvent();
        var registry = new CollectibleDispatchTypes.Registry([eventType]);
        cache.Get(registry, typeof(string), null, types => [.. types]);
        return (new WeakReference(eventType), registry);
    }
}
