// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

public class when_the_types_are_collectible : Specification
{
    readonly EventTypeSetCache<object> _cache = new();
    WeakReference _artifact;
    WeakReference _eventType;
    object _cached;
    object _again;

    void Because()
    {
        (_artifact, _eventType) = CacheForCollectibleTypes(_cache);
        for (var attempt = 0; attempt < 20 && (_artifact.IsAlive || _eventType.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        _cached = _cache.Get([typeof(string)], typeof(string), typeof(int), _ => new object());
        _again = _cache.Get([typeof(string)], typeof(string), typeof(int), _ => new object());
    }

    [Fact] void should_let_the_artifact_be_collected() => _artifact.IsAlive.ShouldBeFalse();
    [Fact] void should_let_the_event_type_be_collected() => _eventType.IsAlive.ShouldBeFalse();
    [Fact] void should_keep_caching_other_types() => _again.ShouldBeSame(_cached);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Artifact, WeakReference EventType) CacheForCollectibleTypes(EventTypeSetCache<object> cache)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"collectible-{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("collectible");
        var artifact = module.DefineType("CollectibleReducer", TypeAttributes.Public).CreateType();
        var eventType = module.DefineType("CollectibleEvent", TypeAttributes.Public).CreateType();
        cache.Get([eventType, typeof(string)], artifact, typeof(int), _ => new object());
        cache.Get([eventType], typeof(string), typeof(int), _ => new object());
        return (new WeakReference(artifact), new WeakReference(eventType));
    }
}
