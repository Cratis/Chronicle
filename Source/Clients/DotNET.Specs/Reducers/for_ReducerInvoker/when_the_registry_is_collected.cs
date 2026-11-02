// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker;

public class when_the_registry_is_collected : Specification
{
    WeakReference _droppedEventType;
    WeakReference _droppedRegistry;
    WeakReference _liveEventType;
    CollectibleDispatchTypes.Registry _liveRegistry;
    ReducerInvoker _liveInvoker;
    int _droppedMapped;
    bool _collected;

    void Because()
    {
        (_droppedEventType, _droppedRegistry, _droppedMapped) = CreateAndDropInvoker();
        (_liveEventType, _liveRegistry, _liveInvoker) = CreateInvoker();
        _collected = CollectibleDispatchTypes.Collect(_droppedEventType, _droppedRegistry);
    }

    [Fact] void should_have_mapped_the_collectible_event_before_it_was_dropped() => _droppedMapped.ShouldEqual(1);
    [Fact] void should_release_the_registry() => _droppedRegistry.IsAlive.ShouldBeFalse();
    [Fact] void should_release_the_collectible_event_type_of_a_non_collectible_reducer() => _droppedEventType.IsAlive.ShouldBeFalse();
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();
    [Fact] void should_keep_the_event_type_of_a_live_invoker() => _liveEventType.IsAlive.ShouldBeTrue();
    [Fact] void should_keep_a_live_invoker_usable() => _liveInvoker.EventTypes.Select(type => type.Id.Value).ShouldContainOnly("CollectibleEvent");
    [Fact] void should_keep_the_live_registry_reachable() => _liveRegistry.AllClrTypes.Count.ShouldEqual(1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference EventType, WeakReference Registry, int Mapped) CreateAndDropInvoker()
    {
        var (eventType, registry, invoker) = CreateInvoker();
        return (eventType, new WeakReference(registry), invoker.EventTypes.Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference EventType, CollectibleDispatchTypes.Registry Registry, ReducerInvoker Invoker) CreateInvoker()
    {
        var eventType = CollectibleDispatchTypes.DefineEvent();
        var registry = new CollectibleDispatchTypes.Registry([eventType]);
        var invoker = new ReducerInvoker(registry, Substitute.For<IClientArtifactsActivator>(), typeof(Reducer), typeof(Model), nameof(Model));
        return (new WeakReference(eventType), registry, invoker);
    }

    public record Model;
    public class Reducer : IReducerFor<Model>
    {
        public Model Handle(CollectibleDispatchTypes.IKnown @event, Model? current) => new();
    }
}
