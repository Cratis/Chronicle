// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

/// <summary>
/// Two collectible assemblies have independent lifetimes, so no link order keeps either from pinning the other:
/// such a set is built per call, and unloading one context must not wait for the other.
/// </summary>
[Collection(UnloadingCollection.Name)]
public class when_collectible_types_span_assemblies : Specification
{
    readonly EventTypeSetCache<Type[]> _cache = new();
    WeakReference _unloaded;
    (Type EventType, System.Runtime.Loader.AssemblyLoadContext Context) _loaded;
    bool _shared;
    bool _collected;

    void Because()
    {
        _loaded = CollectibleDispatchTypes.LoadEventInUnloadableContext();
        (_unloaded, _shared) = CacheAndUnload(_cache, _loaded.EventType);
        _collected = CollectibleDispatchTypes.Collect(_unloaded);
    }

    [Fact] void should_not_share_the_result() => _shared.ShouldBeFalse();
    [Fact] void should_unload_the_context_the_other_outlives() => _unloaded.IsAlive.ShouldBeFalse();
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();
    [Fact] void should_keep_the_other_context_loaded() => _loaded.EventType.Assembly.IsCollectible.ShouldBeTrue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Context, bool Shared) CacheAndUnload(EventTypeSetCache<Type[]> cache, Type outliving)
    {
        var (eventType, context) = CollectibleDispatchTypes.LoadEventInUnloadableContext();
        var result = cache.Get([outliving, eventType], typeof(string), typeof(int), types => [.. types]);
        var again = cache.Get([eventType, outliving], typeof(string), typeof(int), types => [.. types]);
        context.Unload();
        return (new WeakReference(context), ReferenceEquals(result, again));
    }
}
