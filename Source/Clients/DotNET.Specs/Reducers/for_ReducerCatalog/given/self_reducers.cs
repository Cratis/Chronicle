// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.given;

public class self_reducers : an_explicit_store
{
    void Establish()
    {
        _artifacts.EventTypes.Returns([typeof(OwnershipEvent)]);
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
    }

    void Destroy()
    {
        foreach (var handler in _store?.Reducers.GetAllHandlers() ?? [])
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    protected static Type EmitModel(params Attribute[] attributes)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("SelfReducers").DefineType("EmittedModel", TypeAttributes.Public);
        foreach (var attribute in attributes)
        {
            var constructor = attribute.GetType().GetConstructors().Single();
            type.SetCustomAttribute(new CustomAttributeBuilder(constructor, constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray()));
        }
        var method = type.DefineMethod("Reduce", MethodAttributes.Public | MethodAttributes.Static, type, [typeof(OwnershipEvent), type]);
        method.GetILGenerator().Emit(OpCodes.Ldnull);
        method.GetILGenerator().Emit(OpCodes.Ret);
        return type.CreateType();
    }

    protected Task<IProjectionHandler> RegisterProjection<T>() => _store.Projections.Register<T>(builder => builder.From<OwnershipEvent>());

    [EventType]
    public record OwnershipEvent(int Value);

    public record PlainSelf(int Value)
    {
        public static PlainSelf Reduce(OwnershipEvent @event, PlainSelf? current) => new((current?.Value ?? 0) + @event.Value);
    }

    public record MarkedSelf(int Value)
    {
        public static MarkedSelf Reduce(OwnershipEvent @event, MarkedSelf? current) => new((current?.Value ?? 0) + @event.Value);
    }

    protected static Type EmitClassicFor(Type model)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("ClassicReducers").DefineType("ClassicForSelfModel", TypeAttributes.Public, typeof(object), [typeof(IReducerFor<>).MakeGenericType(model)]);
        var method = type.DefineMethod("Apply", MethodAttributes.Public, model, [typeof(OwnershipEvent), model]);
        method.GetILGenerator().Emit(OpCodes.Ldnull);
        method.GetILGenerator().Emit(OpCodes.Ret);
        return type.CreateType();
    }
}
