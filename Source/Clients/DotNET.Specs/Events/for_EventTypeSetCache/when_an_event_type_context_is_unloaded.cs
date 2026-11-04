// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

/// <summary>
/// The artifact and model are non-collectible, so only the event type's own lifetime can release the shared entry,
/// and the cached result references that event type.
/// </summary>
[Collection(UnloadingCollection.Name)]
public class when_an_event_type_context_is_unloaded : Specification
{
    readonly EventTypeSetCache<Type[]> _cache = new();
    WeakReference _context;
    WeakReference _eventType;
    WeakReference _result;
    bool _sharedWhileLoaded;
    bool _collected;
    Type[] _other;
    Type[] _otherAgain;

    void Because()
    {
        (_context, _eventType, _result, _sharedWhileLoaded) = CacheAndUnload(_cache);
        _collected = CollectibleDispatchTypes.Collect(_context, _eventType, _result);
        _other = _cache.Get([typeof(long)], typeof(string), typeof(int), types => [.. types]);
        _otherAgain = _cache.Get([typeof(long)], typeof(string), typeof(int), types => [.. types]);
    }

    [Fact] void should_share_the_result_while_the_context_is_loaded() => _sharedWhileLoaded.ShouldBeTrue();
    [Fact] void should_unload_the_context() => _context.IsAlive.ShouldBeFalse();
    [Fact] void should_release_the_event_type() => _eventType.IsAlive.ShouldBeFalse();
    [Fact] void should_release_the_shared_result() => _result.IsAlive.ShouldBeFalse();
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();
    [Fact] void should_keep_sharing_other_sets() => _otherAgain.ShouldBeSame(_other);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Context, WeakReference EventType, WeakReference Result, bool Shared) CacheAndUnload(EventTypeSetCache<Type[]> cache)
    {
        var (eventType, context) = CollectibleDispatchTypes.LoadEventInUnloadableContext();
        var result = cache.Get([eventType, typeof(long)], typeof(string), typeof(int), types => [.. types]);
        var again = cache.Get([typeof(long), eventType], typeof(string), typeof(int), types => [.. types]);
        context.Unload();
        return (new WeakReference(context), new WeakReference(eventType), new WeakReference(result), ReferenceEquals(result, again));
    }
}
