// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_and_fingerprinting_fails : given.all_dependencies
{
    readonly List<RegisterReducer> _registrations = [];
    Type _unsupportedReducer;
    Exception _error;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _unsupportedReducer = CreateReducerCallingGlobalMethod();
        AddHandler(_unsupportedReducer, typeof(object), "unsupported");
        AddHandler(typeof(HealthyReducer), typeof(string), "healthy");
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _registrations.Add(message.Content.Value0!));
            return Observable.Empty<ReduceOperationMessage>();
        });
    }

    async Task Because() => _error = await Catch.Exception(_reducers.Register);

    [Fact] void should_exercise_a_normalization_failure() => Catch.Exception(() => ReducerFingerprint.Create(_unsupportedReducer)).ShouldNotBeNull();
    [Fact] void should_not_fail_registration() => _error.ShouldBeNull();
    [Fact] void should_register_every_reducer() => _registrations.Count.ShouldEqual(2);
    [Fact] void should_use_the_conservative_fingerprint() => _registrations.Single(_ => _.Reducer.ReducerId == "unsupported").Reducer.Hash.ShouldEqual(ReducerFingerprint.CreateFallback(_unsupportedReducer));
    [Fact] void should_still_use_the_stable_fingerprint_for_other_reducers() => _registrations.Single(_ => _.Reducer.ReducerId == "healthy").Reducer.Hash.ShouldEqual(ReducerFingerprint.Create(typeof(HealthyReducer)));

    void AddHandler(Type reducerType, Type readModelType, string id)
    {
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(reducerType);
        handler.ReadModelType.Returns(readModelType);
        handler.Id.Returns((ReducerId)id);
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([]);
        handler.IsActive.Returns(true);
        _handlersByModelType[readModelType] = handler;
    }

    static Type CreateReducerCallingGlobalMethod()
    {
        // Valid executable IL can reference a module-level method with no declaring type.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GlobalMethodReducer"), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Main");
        var global = module.DefineGlobalMethod("Apply", MethodAttributes.Public | MethodAttributes.Static, typeof(object), [typeof(object)]);
        var globalIL = global.GetILGenerator();
        globalIL.Emit(OpCodes.Ldarg_0);
        globalIL.Emit(OpCodes.Ret);
        module.CreateGlobalFunctions();

        var reducer = module.DefineType("Reducer", TypeAttributes.Public);
        reducer.AddInterfaceImplementation(typeof(IReducerFor<object>));
        var reduce = reducer.DefineMethod("Reduce", MethodAttributes.Public, typeof(object), [typeof(int), typeof(object)]);
        var reduceIL = reduce.GetILGenerator();
        reduceIL.Emit(OpCodes.Ldarg_2);
        reduceIL.Emit(OpCodes.Call, global.MetadataToken);
        reduceIL.Emit(OpCodes.Ret);
        return reducer.CreateType();
    }

    class HealthyReducer : IReducerFor<string>;
}
