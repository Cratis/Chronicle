// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker;

/// <summary>
/// Control for the model-keyed side of discovery: a collectible reducer and model must unload once the invoker
/// and registry are dropped, so the dispatch map never pins the assembly that declares them.
/// </summary>
[Collection(UnloadingCollection.Name)]
public class when_the_model_is_collectible : Specification
{
    WeakReference _model;
    WeakReference _reducer;
    int _mapped;
    bool _collected;

    void Because()
    {
        (_model, _reducer, _mapped) = CreateAndDropInvoker();
        _collected = CollectibleDispatchTypes.Collect(_model, _reducer);
    }

    [Fact] void should_have_mapped_the_event_before_it_was_dropped() => _mapped.ShouldEqual(1);
    [Fact] void should_release_the_model() => _model.IsAlive.ShouldBeFalse();
    [Fact] void should_release_the_reducer() => _reducer.IsAlive.ShouldBeFalse();
    [Fact] void should_collect_everything_it_waited_for() => _collected.ShouldBeTrue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Model, WeakReference Reducer, int Mapped) CreateAndDropInvoker()
    {
        var module = CollectibleDispatchTypes.DefineModule();
        var modelBuilder = module.DefineType("CollectibleModel", TypeAttributes.Public);
        modelBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        var model = modelBuilder.CreateType();

        var reducerBuilder = module.DefineType("CollectibleReducer", TypeAttributes.Public);
        reducerBuilder.DefineDefaultConstructor(MethodAttributes.Public);
        var handle = reducerBuilder.DefineMethod("Handle", MethodAttributes.Public, model, [typeof(CollectibleDispatchTypes.IKnown), model]);
        var il = handle.GetILGenerator();
        il.Emit(OpCodes.Newobj, model.GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);
        var reducer = reducerBuilder.CreateType();

        var registry = new CollectibleDispatchTypes.Registry([typeof(Known)]);
        var invoker = new ReducerInvoker(registry, Substitute.For<IClientArtifactsActivator>(), reducer, model, "CollectibleModel");
        return (new WeakReference(model), new WeakReference(reducer), invoker.EventTypes.Count);
    }

    public record Known : CollectibleDispatchTypes.IKnown;
}
