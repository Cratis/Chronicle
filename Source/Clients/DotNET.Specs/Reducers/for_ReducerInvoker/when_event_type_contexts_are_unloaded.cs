// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker;

/// <summary>
/// A non-collectible reducer dispatching to event types from unloadable contexts, each context shared by two
/// registries: the process-wide dispatch maps must not keep any context loaded once its registries are dropped.
/// </summary>
[Collection(UnloadingCollection.Name)]
public class when_event_type_contexts_are_unloaded : Specification
{
    const int Contexts = 5;
    WeakReference[] _contexts;
    int _mapped;
    bool _collected;

    void Because()
    {
        (_contexts, _mapped) = CreateAndUnload();
        _collected = CollectibleDispatchTypes.Collect(_contexts);
    }

    [Fact] void should_have_mapped_every_event_before_unloading() => _mapped.ShouldEqual(Contexts * 2);
    [Fact] void should_unload_every_context() => _contexts.Count(context => context.IsAlive).ShouldEqual(0);
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference[] Contexts, int Mapped) CreateAndUnload()
    {
        var contexts = new WeakReference[Contexts];
        var mapped = 0;
        for (var index = 0; index < Contexts; index++)
        {
            var (eventType, context) = CollectibleDispatchTypes.LoadEventInUnloadableContext();
            mapped += Invoker(eventType).EventTypes.Count + Invoker(eventType).EventTypes.Count;
            context.Unload();
            contexts[index] = new WeakReference(context);
        }

        return (contexts, mapped);
    }

    static ReducerInvoker Invoker(Type eventType) =>
        new(new CollectibleDispatchTypes.Registry([eventType]), Substitute.For<IClientArtifactsActivator>(), typeof(Reducer), typeof(Model), nameof(Model));

    public record Model;
    public class Reducer : IReducerFor<Model>
    {
        public Model Handle(CollectibleDispatchTypes.IKnown @event, Model? current) => new();
    }
}
